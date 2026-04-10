# Asset Monitor

Aplicação de console em C# desenvolvida para monitorar a cotação de ativos da B3 e enviar alertas por e-mail quando o preço atinge níveis pré-configurados de compra ou venda.

## Requisitos Atendidos
- Desenvolvido em C# (.NET Core).
- Aplicação executada em linha de comando.
- Recebe 3 parâmetros via CLI: Ativo, Preço de Venda e Preço de Compra.
- Lê configurações de e-mail (destinatário, remetente e SMTP) através de um arquivo `appsettings.json`.
- Realiza o monitoramento contínuo do ativo especificado.
- Dispara e-mails de alerta aconselhando a venda (quando preço é maior que a referência) ou a compra (quando preço é menor que a referência).

## Arquitetura e Padrões Utilizados
O projeto foi estruturado com foco em manutenibilidade e testes, aplicando as seguintes práticas:
- Arquitetura inspirada em Domain-Driven Design (DDD), dividida em Application, Domain e Infrastructure.
- Injeção de Dependências utilizando as abstrações nativas do .NET.
- Padrão estrutural Composite (`CompositeStockProvider`), facilitando a adição futura de múltiplas APIs de cotação para garantir redundância (fallback).
- Tratamento de encerramento seguro (Graceful Shutdown) através de CancellationToken.
- Implementação de um mecanismo opcional de cooldown para evitar disparos duplicados em curtos períodos de oscilação do mercado.
- Cobertura de testes unitários utilizando xUnit e Moq.

## Configuração

Antes de iniciar a aplicação, configure as credenciais necessárias no arquivo `appsettings.json` localizado no diretório de execução:

```json
{
  "Brapi": {
    "Token": "SEU_TOKEN_BRAPI"
  },
  "EmailSettings": {
    "Recipient": "email.destino@exemplo.com",
    "SmtpServer": "smtp.seuhost.com",
    "SmtpPort": 587,
    "Username": "seu.email@exemplo.com",
    "Password": "sua_senha"
  },
  "MonitorSettings": {
    "AlertCooldownMinutes": 0
  }
}