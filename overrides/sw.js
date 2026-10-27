// sw.js - service worker: keeps the game's downloads in the browser (Cache
// Storage), so a visit only downloads what changed on the web server since the
// last one. Registered by the page (web\shell.html); copied next to it by
// web\make_site.py.
//  - data/<file>: data_manifest.txt has a content hash per file (third column,
//    web\make_manifest.py / make_site.py). A stored copy with the same hash is used
//    without asking the server; a changed hash downloads the file again.
//  - data_startup.pack, data_manifest.txt: the stored copy is checked with the
//    server (ETag / Last-Modified) - a few hundred bytes - and downloaded again
//    only when it changed.
// Everything else (page, game.js, game.wasm, ws.ashx) goes to the network as before.
// Downloads from the network are announced on BroadcastChannel 'pt-downloads' for
// the page's progress bar (web\progress.js): { t: 'start', id, rel, size },
// { t: 'bytes', id, got }, { t: 'end', id, ok }.
var CACHE = 'pt-data-1';				// change to drop every stored file
var CHECKED = ['data_startup.pack', 'data_manifest.txt'];
var MANIFEST = 'data_manifest.txt';
var scopePath = new URL(self.registration.scope).pathname;

var channel = self.BroadcastChannel ? new BroadcastChannel('pt-downloads') : null;
var nextId = 0;
// res.body, counted as it passes; announced as a download of rel
function counted(res, rel, size) {
	if (!channel || !res.body || !self.TransformStream) return res.body;
	var id = ++nextId, got = 0, last = 0;
	channel.postMessage({ t: 'start', id: id, rel: rel, size: size || +res.headers.get('Content-Length') || 0 });
	return res.body.pipeThrough(new TransformStream({
		transform: function (chunk, ctl) {
			got += chunk.length;
			var now = Date.now();
			if (now - last > 150) { last = now; channel.postMessage({ t: 'bytes', id: id, got: got }); }
			ctl.enqueue(chunk);
		},
		flush: function () { channel.postMessage({ t: 'end', id: id, ok: true }); }
	}));
}

self.addEventListener('install', function () { self.skipWaiting(); });
self.addEventListener('activate', function (e) {
	e.waitUntil(caches.keys().then(function (names) {
		return Promise.all(names.filter(function (n) { return n.indexOf('pt-data-') == 0 && n != CACHE; })
			.map(function (n) { return caches.delete(n); }));
	}).then(function () { return self.clients.claim(); }));
});

self.addEventListener('fetch', function (e) {
	var req = e.request;
	if (req.method != 'GET' || req.headers.has('Range')) return;
	var url = new URL(req.url);
	if (url.origin != location.origin || url.search || url.pathname.indexOf(scopePath) != 0) return;
	var rel = url.pathname.slice(scopePath.length);
	if (rel.indexOf('data/') == 0) e.respondWith(dataFile(e, decodeURIComponent(rel.slice(5))));
	else if (CHECKED.indexOf(rel) >= 0) e.respondWith(checked(e, rel));
});

// rel path -> { size, hash }, from the stored manifest (read again after the worker restarts)
var versions = null;
function parseManifest(text) {
	var m = new Map();
	text.split('\n').forEach(function (l) {
		var a = l.split('\t');
		if (a.length > 2) m.set(a[0], { size: +a[1], hash: a[2].trim() });
	});
	return m;
}
function getVersions() {
	if (!versions) versions = caches.open(CACHE)
		.then(function (c) { return c.match(new URL(MANIFEST, self.registration.scope).href); })
		.then(function (r) { return r ? r.text() : ''; })
		.then(parseManifest)
		.catch(function () { return new Map(); });
	return versions;
}

async function dataFile(e, rel) {
	var entry = (await getVersions()).get(rel);
	if (!entry) return fetch(e.request);			// not listed (or old manifest without hashes)
	var ver = entry.hash;
	var cache = await caches.open(CACHE);
	var hit = await cache.match(e.request.url);
	if (hit && hit.headers.get('X-PT-Version') == ver) return hit;
	// 'no-cache': the browser's own cache may still hold the old copy (data\web.config max-age)
	var res = await fetch(e.request.url, { cache: 'no-cache' });
	if (res.status != 200) return res;
	var body = await new Response(counted(res, rel, entry.size)).arrayBuffer();
	var h = new Headers(res.headers);
	h.delete('Content-Encoding');
	h.set('Content-Length', String(body.byteLength));
	h.set('X-PT-Version', ver);
	var out = new Response(body, { status: 200, statusText: 'OK', headers: h });
	e.waitUntil(cache.put(e.request.url, out.clone()).catch(function () {}));	// storage full: still played
	return out;
}

async function checked(e, rel) {
	var cache = await caches.open(CACHE);
	var hit = await cache.match(e.request.url);
	var cond = {};
	if (hit) {
		if (hit.headers.get('ETag')) cond['If-None-Match'] = hit.headers.get('ETag');
		if (hit.headers.get('Last-Modified')) cond['If-Modified-Since'] = hit.headers.get('Last-Modified');
	}
	var res;
	try { res = await fetch(e.request.url, { cache: 'no-store', headers: cond }); }
	catch (err) { if (hit) return hit; throw err; }		// server unreachable: use the stored copy
	if (res.status == 304 && hit) return hit;
	if (res.status != 200) return res;
	if (rel == MANIFEST) {
		var text = res.clone().text();
		versions = text.then(parseManifest);
		e.waitUntil(cache.put(e.request.url, res.clone()).then(function () { return versions; }).then(prune).catch(function () {}));
	}
	else {
		e.waitUntil(cache.put(e.request.url, res.clone()).catch(function () {}));
		// The fetch API exposes a decoded body even if the upstream sent gzip/br.
		// A reconstructed Response must NOT claim that decoded bytes are still
		// Content-Encoding:gzip or use the smaller encoded Content-Length.
		var headers = new Headers(res.headers);
		headers.delete('Content-Encoding');
		headers.delete('Content-Length');
		var decoded = new Response(res.body, { status: res.status, statusText: res.statusText, headers: headers });
		return new Response(counted(decoded, rel), { status: res.status, statusText: res.statusText, headers: headers });
	}
	return res;
}

// data files no longer in the manifest
async function prune(man) {
	var cache = await caches.open(CACHE);
	var base = new URL('data/', self.registration.scope).href;
	var keys = await cache.keys();
	for (var i = 0; i < keys.length; i++) {
		var u = keys[i].url;
		if (u.indexOf(base) == 0 && !man.has(decodeURIComponent(u.slice(base.length)))) await cache.delete(keys[i]);
	}
}
