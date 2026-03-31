# Integração Pluggy (Open Finance) - PersonalLedger

Este documento detalha a arquitetura assíncrona orientada a eventos construída para integrar o PersonalLedger com a API do Pluggy, permitindo a sincronização de extratos bancários de forma resiliente, rápida e em conformidade com o **Clean Architecture**.

---

## 🏗️ 1. Visão Geral da Arquitetura

O ecossistema foi desenhado para suportar o download massivo de transações bancárias (carga inicial e atualizações diárias) sem onerar a API principal. Para isso, o fluxo foi dividido em três frentes:

1. **Frontend / Mobile (Autorização):** Pede permissão temporária (*Connect Token*) para abrir o "modal/widget" onde o usuário digita a senha do banco.
2. **Webhooks (Gatilho em Tempo Real):** A API atua como um ouvinte rápido, apenas confirmando o recebimento da notificação do Pluggy.
3. **MassTransit / RabbitMQ (Worker):** Toda a inteligência e o peso da regra de negócio (baixar páginas e mais páginas do histórico de transações) ocorrem em planos de fundo.

---

## ⚙️ 2. Componentes e Responsabilidades

### A. Camada de Infraestrutura (`PersonalLedger.Infrastructure`)
É onde a comunicação com o mundo externo acontece.
* **`IPluggyApi.cs` (Refit):** Define a assinatura das rotas oficiais do Pluggy. Suporta autenticação baseada em API Key e *Query Parameters* dinâmicos (`from`, `to`, `page`) para paginação extensa do extrato.
* **`PluggyService.cs`:** Serviço envelopador que interage pesadamente com a Interface do Refit. É responsável por:
  * Manter a `API_Key` Mestre em cache (visto que ela tem validade rotativa).
  * Embute nosso `clientUserId` original na hora de autorizar a conexão do usuário (*isso é vital para rastrearmos de quem são as transações quando elas voltarem pelo webhook*).

### B. Camada de API (`PersonalLedger.Api`)
* **`PluggyController.cs`:** Expõe o endpoint `GET /api/pluggy/token?clientUserId={id}`. É acionado estritamente pelo front-end para pegar a licença provisória de renderizar o Widget bancário na tela do usuário.
* **`PluggyWebhookController.cs`:** Endpoint ultrarrápido (`POST /api/webhooks/pluggy`). A responsabilidade dele é unicamente receber a notificação do Pluggy (ex: `transaction.created` ou `item.updated`), extrair o `clientUserId` enxertado, converter para `Guid UserId`, e lançar a mensagem no barramento do RabbitMQ. ***Ele devolve Status HTTP 200 intantaneamente para evitar Timeout na Pluggy.***

### C. Camada de Mensageria & Consumer (`MassTransit`)
* **`SyncAccountMessage.cs`:** Contrato passivo `(UserId, ItemId)` definido dentro de `PersonalLedger.Application/Common/Messaging/`. Mantém o domínio agnóstico de tecnologias.
* **`SyncAccountConsumer.cs`:** Coração da Regra de Negócio de Sincronização. Alocado propositalmente na camada de Infrastructure para não poluir o *Core*.  
  **O que ele faz:**
  1. Ao capturar o contrato da fila do RabbitMQ, aciona o `IPluggyService`.
  2. Executa um laço (`do/while`) paginando de forma agressiva as transações de até 6 meses para trás (Carga Inicial / *Retroative Sync*).
  3. Mapeia e traduz o contrato do banco externo usando suas regras de negócio (ex: Valores Positivos = `ENTRADA` / Negativos = `SAÍDA`).
  4. Encaminha as Entities traduzidas para gravação no `ApplicationDbContext`. Caso ocorra qualquer queda de rede externa, o MassTransit estorna a mensagem na fila automaticamente impedindo perda de extrato (Resiliência / Sensação de falha suave).

---

## 🔄 3. O Fluxo de Carga Inicial (Exemplo Prático)

Este é o desenho passo-a-passo de quando um usuário conecta a conta dele do "Itaú" pela primeira vez através do app:

1. App chama `/api/pluggy/token?clientUserId=UserA`.
2. App lança Widget do Pluggy com o token; o usuário digita senha e aprova.
3. O servidor Pluggy vincula o Itaú ao identificador mágico `Item_1234`.
4. O servidor do Pluggy internamente gasta entre 1 e 5 minutos extraindo todos os dados retroativos do Itau.
5. Quando o Pluggy acaba de baixar as informações no banco deles, ele dá o *"ping"* na rota do nosso Webhook publicando `{ event: "item.updated", itemId: "Item_1234", clientUserId: "UserA" }`.
6. A nossa API transforma em `SyncAccountMessage` e manda para a fila do Rabbit.
7. O nosso **`SyncAccountConsumer`** acorda isolado, absorve essa mensagem, itera `N` páginas do histórico, separa despesas/receitas, e salva no SQL Server sem o App dar *freeze*.
