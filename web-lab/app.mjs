import { StormPTGatewayClient, StormPTGatewayError } from "./vendor/StormPTGatewayClient.mjs";

const loginView = document.getElementById("login-view");
const charactersView = document.getElementById("characters-view");
const loginForm = document.getElementById("login-form");
const loginButton = document.getElementById("login-button");
const logoutButton = document.getElementById("logout-button");
const usernameInput = document.getElementById("username");
const passwordInput = document.getElementById("password");
const statusNode = document.getElementById("status");
const accountTitle = document.getElementById("account-title");
const characterList = document.getElementById("character-list");
const emptyList = document.getElementById("empty-list");

let gateway = null;
let reservedCharacter = null;
let reservationButtons = [];

function status(message, kind = "info") {
  statusNode.textContent = message;
  statusNode.dataset.kind = kind;
}

function busy(flag) {
  loginButton.disabled = flag;
  logoutButton.disabled = flag;
}

function loginScreen() {
  loginView.hidden = false;
  charactersView.hidden = true;
  accountTitle.textContent = "Sua conta";
  characterList.replaceChildren();
  reservedCharacter = null;
  reservationButtons = [];
}

function updateReservationButtons() {
  for (const { name, button } of reservationButtons) {
    button.disabled = !!reservedCharacter && reservedCharacter !== name;
    button.textContent = reservedCharacter === name ? "Liberar" : "Reservar";
  }
}

function renderCharacters(username, characters) {
  characterList.replaceChildren();
  reservationButtons = [];
  for (const character of characters) {
    const item = document.createElement("li");
    const details = document.createElement("div");
    const name = document.createElement("strong");
    name.textContent = character.name;
    const role = document.createElement("span");
    role.textContent = character.className;
    const level = document.createElement("span");
    level.className = "level";
    level.textContent = "Nível " + character.level;
    details.append(name, role);
    const action = document.createElement("button");
    action.type = "button";
    action.className = "char-action";
    action.textContent = "Reservar";
    action.setAttribute?.("aria-label", "Reservar personagem " + character.name);
    action.addEventListener("click", async () => {
      if (!gateway || action.disabled) return;
      const same = reservedCharacter === character.name;
      action.disabled = true;
      try {
        if (same) {
          await gateway.releaseCharacter(character.name);
          reservedCharacter = null;
          status("Reserva liberada. O personagem permanece fora do mapa.", "ok");
        } else {
          const lease = await gateway.reserveCharacter(character.name);
          if (lease.worldAccessGranted !== false) throw new Error("Invalid lease.");
          reservedCharacter = character.name;
          status("Personagem reservado por " + lease.expiresInSeconds +
                 " segundos. A entrada no mundo ainda não existe.", "ok");
        }
      } catch (error) {
        if (same && error instanceof StormPTGatewayError && error.status === 409)
          reservedCharacter = null; // Already expired or released.
        status(error instanceof StormPTGatewayError && error.status === 409
          ? "Reserva indisponível ou ocupada por outra sessão."
          : "Não foi possível atualizar a reserva no LAB.", "error");
      } finally {
        updateReservationButtons();
      }
    });
    reservationButtons.push({ name: character.name, button: action });
    item.append(details, level, action);
    characterList.append(item);
  }
  updateReservationButtons();
  accountTitle.textContent = "Conta: " + username;
  emptyList.hidden = characters.length !== 0;
  loginView.hidden = true;
  charactersView.hidden = false;
}

async function checkGateway() {
  busy(true);
  if (!gateway) {
    status("Abra esta página por HTTPS ou localhost para acessar o laboratório.", "error");
    return;
  }
  try {
    const response = await fetch("/health", {
      credentials: "omit", cache: "no-store", redirect: "error",
      headers: { Accept: "application/json" }
    });
    if (!response.ok) throw new Error("Gateway não respondeu.");
    const health = await response.json();
    if (health.status !== "ok" || health.realLoginConfigured !== true) {
      status("LAB conectado, mas o login real permanece desativado por segurança.", "error");
      return;
    }
    busy(false);
    status("Gateway do LAB disponível para contas de teste.", "ok");
  } catch {
    status("Gateway do LAB indisponível. Nenhuma conta foi acessada.", "error");
  }
}

loginForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (!gateway || loginButton.disabled) return;
  busy(true);
  status("Validando a conta de laboratório...");
  const account = usernameInput.value.trim();
  const password = passwordInput.value;
  passwordInput.value = "";
  try {
    await gateway.login(account, password);
    const identity = await gateway.me();
    const data = await gateway.characters();
    if (identity.username.toLowerCase() !== data.username.toLowerCase()) {
      throw new StormPTGatewayError("Identidade da sessão não coincide com o perfil.", 403);
    }
    renderCharacters(identity.username, data.characters);
    status("Personagens de laboratório carregados. O acesso ao mapa ainda não existe.", "ok");
  } catch (error) {
    loginScreen();
    try { if (gateway.authenticated) await gateway.logout(); } catch { /* token expires */ }
    if (error instanceof StormPTGatewayError) {
      status(error.status === 401 ? "Conta de teste inválida ou sessão expirada." :
        error.status === 429 ? "Muitas tentativas. Tente novamente mais tarde." :
        "Não foi possível autenticar no LAB: " + error.message, "error");
    } else {
      status("Falha inesperada ao consultar o LAB.", "error");
    }
  } finally {
    busy(false);
  }
});

logoutButton.addEventListener("click", async () => {
  busy(true);
  try {
    if (gateway) await gateway.logout();
  } catch { /* Local session is cleared by SDK even if server unavailable. */ }
  finally {
    loginScreen();
    passwordInput.value = "";
    status("Sessão encerrada neste navegador.", "ok");
    busy(false);
  }
});

try {
  gateway = new StormPTGatewayClient();
} catch {
  gateway = null;
}
loginScreen();
void checkGateway();
