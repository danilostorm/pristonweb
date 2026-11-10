import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

class Element {
  constructor() {
    this.hidden = false;
    this.disabled = false;
    this.value = "";
    this.textContent = "";
    this.dataset = {};
    this.children = [];
    this.listeners = new Map();
  }
  addEventListener(event, handler) { this.listeners.set(event, handler); }
  append(...items) { this.children.push(...items); }
  replaceChildren(...items) { this.children = items; }
  async emit(name, data) {
    const handler = this.listeners.get(name);
    assert.ok(handler, "listener " + name);
    return handler(data);
  }
}

const json = (content, status = 200) =>
  new Response(JSON.stringify(content), { status, headers: { "Content-Type": "application/json" } });

test("pagina contém formulário acessível e indicação clara de LAB", async () => {
  const html = await readFile(new URL("../index.html", import.meta.url), "utf8");
  assert.match(html, /<html lang="pt-BR">/);
  assert.match(html, /id="login-form"/);
  assert.match(html, /type="password"/);
  assert.match(html, /autocomplete="current-password"/);
  assert.match(html, /id="character-list"/);
  assert.match(html, /role="status"/);
  assert.match(html, /object-src 'none'/);
  assert.match(html, /Protótipo de autenticação/);
});

test("login e personagens de LAB renderizam sem persistir credenciais", async () => {
  const nodes = new Map();
  for (const id of [
    "login-view", "characters-view", "login-form", "login-button", "logout-button",
    "username", "password", "status", "account-title", "character-list", "empty-list",
  ]) nodes.set(id, new Element());

  const requests = [];
  globalThis.document = {
    getElementById: (id) => {
      assert.ok(nodes.has(id), "node " + id);
      return nodes.get(id);
    },
    createElement: () => new Element(),
  };
  globalThis.location = {
    protocol: "https:", hostname: "lab.invalid", origin: "https://lab.invalid",
  };
  globalThis.fetch = async (path, init) => {
    requests.push({ path, init });
    if (path === "/health") return json({ status: "ok", realLoginConfigured: true });
    if (path === "/v1/sessions") {
      assert.deepEqual(JSON.parse(init.body), { username: "TEST1", password: "dummyPassword!" });
      return json({ access_token: "Q".repeat(43), token_type: "Bearer", expires_in: 900 });
    }
    if (path === "/v1/me") return json({ username: "TEST1" });
    if (path === "/v1/characters") return json({ username: "TEST1", characters: [
      { name: "<invalid html> Knight", classCode: 1, className: "Fighter", level: 42 },
    ] });
    if (path === "/v1/characters/reserve") {
      assert.equal(JSON.parse(init.body).name, "<invalid html> Knight");
      return json({ name: "<invalid html> Knight", expiresInSeconds: 90,
        worldAccessGranted: false });
    }
    if (path === "/v1/characters/release") {
      assert.equal(JSON.parse(init.body).name, "<invalid html> Knight");
      return new Response(null, { status: 204 });
    }
    if (path === "/v1/logout") return new Response(null, { status: 204 });
    throw new Error("unexpected route");
  };

  await import("../app.mjs?test-page");
  await new Promise(resolve => setTimeout(resolve, 10));
  assert.equal(nodes.get("login-button").disabled, false);
  nodes.get("username").value = "TEST1";
  nodes.get("password").value = "dummyPassword!";
  await nodes.get("login-form").emit("submit", { preventDefault() {} });
  assert.equal(nodes.get("password").value, "", "password cleared");
  assert.equal(nodes.get("login-view").hidden, true);
  assert.equal(nodes.get("characters-view").hidden, false);
  assert.equal(nodes.get("character-list").children.length, 1);
  const character = nodes.get("character-list").children[0];
  assert.equal(character.children[0].children[0].textContent, "<invalid html> Knight",
               "character treated as textContent");
  assert.equal(nodes.get("account-title").textContent, "Conta: TEST1");
  assert.equal(requests.some(x => x.path === "/v1/sessions"), true);

  const reserveButton = character.children[2];
  assert.equal(reserveButton.textContent, "Reservar");
  await reserveButton.emit("click");
  assert.equal(reserveButton.textContent, "Liberar");
  assert.match(nodes.get("status").textContent, /entrada no mundo ainda não existe/);
  await reserveButton.emit("click");
  assert.equal(reserveButton.textContent, "Reservar");

  await nodes.get("logout-button").emit("click");
  assert.equal(nodes.get("login-view").hidden, false);
  assert.equal(nodes.get("character-list").children.length, 0);
  assert.match(nodes.get("status").textContent, /encerrada/);
});
