# desafio-inoa
Repositório criado para o desafio do PS da INOA.

# O que é o desafio?
Um aplicativo console em C#/.NET que monitora a cotação de um ativo da B3 (Bolsa de Valores do Brasil) em tempo real (a cada 30 segundos). Quando o preço atinge um limite de compra ou venda pré-definido, ele dispara um alerta via e-mail.

# Pré-requisitos para rodar
Para executar este projeto, você precisará ter o .NET SDK instalado em sua máquina.
- .NET SDK (versão 6.0 ou superior)

# Como rodar?
1. Obter a Chave da API
- Este projeto utiliza a API brapi.dev para buscar as cotações. Você precisará de uma chave de API: 
-> Acesse brapi.dev
-> Obtenha sua chave gratuita.

2. Configurar o Arquivo .env (Credenciais)
Crie um arquivo chamado .env na raiz do projeto (na mesma pasta do MonitorB3.csproj), ele vai armazenar as credenciais de email (o .gitignore já está configurado para ignorá-lo).
- Preencha o arquivo com suas credenciais de e-mail e a chave da API:
SmtpConfig__ApiKey="SUA_CHAVE_AQUI"
SmtpConfig__FromEmail="SEU_EMAIL_DE_ENVIO@gmail.com"
SmtpConfig__Username="SEU_EMAIL_DE_ENVIO@gmail.com"
SmtpConfig__Password="SUA_SENHA_DE_APP_AQUI"

3. Execução do monitor
O programa exige 4 argumentos de linha de comando: o ativo, o preço de venda, o preço de compra e o e-mail de destino.
- Comandos:
-> cd MonitorB3
-> dotnet run -- <ATIVO> <PRECO_VENDA> <PRECO_COMPRA> <EMAIL_DESTINO>

- Exemplo:
O exemplo abaixo monitora a PETR4. Se o preço subir para R$ 30,00 ou mais, ele enviará um alerta de VENDA para seu.amigo@email.com. Se cair para R$ 25,00 ou menos, enviará um alerta de COMPRA.
-> dotnet run -- PETR4 30.00 25.00 seu.amigo@email.com

OBS: Se a execução for bem-sucedida, você verá o loop de monitoramento começar no console