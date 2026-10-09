## 🛠️ Pré-requisitos de Instalação

Para rodar e editar o projeto, certifique-se de ter os seguintes requisitos não funcionais configurados[cite: 6]:
* **Unity** (Versão 6000.3.14f1, instalada via Unity Hub)[cite: 6].
* **Visual Studio Code** (com extensão para a linguagem C#)[cite: 6].
* **Aplicativo Meta Quest Link** instalado e logado no PC.
* **Meta Quest** (ou outro visor VR compatível com conexão para PC)[cite: 6].

---

## ⚙️ Configuração Inicial do Projeto

1. Clone o repositório na sua máquina executando `git clone <URL_DO_SEU_REPOSITORIO>` no terminal.
2. Abra o Unity Hub, clique em **Add** e selecione a pasta do projeto.
3. **Correção de Input do Simulador:** Vá em `Edit > Project Settings > Player > Other Settings`. Role até **Active Input Handling** e garanta que está como **Both**. Reinicie a Unity se precisar alterar.

---

## 💻 Testando com o Meta XR Simulator (Sem Óculos)

1. No menu do topo da Unity, vá em `Oculus > Meta XR Simulator` e ative-o.
2. Na aba de configurações do simulador, em **Input Settings**, mude *Left input* e *Right input* para **Controller**.
3. Em **Keyboard and mouse**, ative **Point & click** (Action input: Right - Controller).
4. Dê **Play** na Unity e clique uma vez na aba **Game** para focar o mouse.
5. **Comandos básicos:** 
   * **Visão:** Segure o botão direito do mouse e arraste.
   * **Movimento:** Teclas W, A, S, D.
   * **Controle Direito:** Segure a barra de **Espaço**.
   * **Gatilho:** Com o Espaço pressionado, dê um **clique esquerdo** no mouse.

---

## 🥽 Testando com o Meta Quest Físico (Cabo USB-C)

1. Ative o **Modo do Desenvolvedor** no seu Meta Quest através do aplicativo da Meta no celular.
2. Conecte o Meta Quest ao computador usando um cabo USB-C 3.0.
3. Coloque o visor e selecione **Permitir** na mensagem "Permitir depuração USB" ou "Acessar dados".
4. No menu rápido do Quest, ative o **Quest Link** para conectar ao PC.
5. Na tela do PC, clique no botão **Play** (▶️) na Unity.
6. Coloque o visor de volta para visualizar a simulação em Realidade Virtual.
