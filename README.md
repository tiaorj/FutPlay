# FutPlay ⚽

FutPlay é um sistema web desenvolvido em **ASP.NET Core MVC** para acompanhamento de campeonatos de futebol, jogos, classificações, ligas, palpites e rankings.

O projeto foi criado com foco em estudo, portfólio e evolução prática de uma aplicação real usando **.NET, Entity Framework Core, SQL Server, Identity, camada de Services e integração com API externa de futebol**.

---

## 🚀 Objetivo do Projeto

O objetivo do FutPlay é funcionar como um portal de futebol com duas áreas principais:

### Área pública

Usuários sem login podem visualizar:

- Campeonatos
- Times e seleções
- Jogos
- Resultados
- Classificação dos campeonatos
- Portal público do campeonato
- Rankings públicos de ligas

### Área logada

Usuários autenticados podem:

- Criar e editar campeonatos
- Criar e editar times
- Criar e editar jogos
- Criar ligas
- Participar de ligas
- Enviar palpites
- Acompanhar rankings
- Importar dados de uma API de futebol
- Sincronizar resultados, classificação e pontuação

---

## 🛠️ Tecnologias Utilizadas

- ASP.NET Core MVC
- C#
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity
- Razor Views
- Bootstrap
- HTML5
- CSS3
- API-Football / API-Sports
- Visual Studio 2022
- Git e GitHub

---

## 📌 Funcionalidades Implementadas

### Portal público

- Página inicial com visão geral do sistema
- Campeonatos em destaque
- Próximos jogos
- Últimos resultados
- Ligas públicas
- Top participantes
- Acesso público a campeonatos, jogos e classificação

### Campeonatos

- Cadastro de campeonatos
- Edição de campeonatos
- Inativação de campeonatos
- Portal público do campeonato
- Classificação por campeonato
- Classificação por grupo
- Suporte a país e logo do campeonato
- Integração com ID externo da API

### Times / Seleções

- Cadastro de times e seleções
- Edição de times
- Visualização pública dos times
- Suporte a escudo/logo via URL
- Integração com ID externo da API

### Jogos

- Cadastro de jogos
- Edição de jogos
- Visualização pública dos jogos
- Registro de resultado
- Status do jogo
- Fase, grupo e rodada
- Integração com ID externo da API

Status suportados:

- Agendado
- Em andamento
- Finalizado
- Cancelado

### Ligas

- Criação de ligas públicas e privadas
- Código de convite
- Ranking por liga
- Tela de palpites por liga
- Participantes por liga

### Participantes

- Cadastro de participantes por liga
- Nome e e-mail do participante
- Controle de pontuação total
- Status ativo/inativo

### Palpites

- Cadastro de palpites por participante
- Palpite de placar
- Bloqueio de palpite após o início do jogo
- Cálculo automático de pontuação
- Ranking atualizado com base nos resultados

---

## 🧮 Regra de Pontuação dos Palpites

A regra inicial de pontuação é:

| Situação | Pontos |
|---|---:|
| Placar exato | 10 |
| Acertou vencedor ou empate | 5 |
| Acertou gols do time da casa | +2 |
| Acertou gols do visitante | +2 |
| Errou tudo | 0 |

Exemplo:

Resultado real:

```text
Brasil 2 x 1 Argentina
```

Palpites:

```text
Brasil 2 x 1 Argentina = 10 pontos
Brasil 1 x 0 Argentina = 5 pontos
Brasil 2 x 0 Argentina = 7 pontos
```

---

## 🏆 Classificação dos Campeonatos

A classificação é recalculada automaticamente com base nos jogos finalizados.

Critérios utilizados:

1. Pontos
2. Vitórias
3. Saldo de gols
4. Gols pró
5. Grupo
6. Posição

Regra de pontuação da classificação:

| Resultado | Pontos |
|---|---:|
| Vitória | 3 |
| Empate | 1 |
| Derrota | 0 |

A classificação considera apenas jogos com:

```text
Status = Finalizado
GolsCasa preenchido
GolsVisitante preenchido
Ativo = true
```

---

## 🔄 Integração com API de Futebol

O FutPlay possui integração com uma API externa de futebol para buscar e importar dados reais.

Funcionalidades disponíveis:

- Buscar campeonatos
- Importar campeonatos
- Importar times
- Importar jogos
- Atualizar resultados
- Sincronizar campeonato

A sincronização executa:

1. Atualização dos resultados pela API
2. Recálculo da classificação
3. Recálculo dos pontos dos palpites
4. Atualização do ranking dos participantes

---

## ⚠️ Limitação da API no Plano Gratuito

O plano gratuito da API pode possuir:

- Limite diário de requisições
- Restrição de temporadas disponíveis
- Retorno limitado para algumas competições

Por esse motivo, o FutPlay também possui uma área de **Dados de Teste**, permitindo testar o sistema sem consumir chamadas da API.

---

## 🧪 Dados Mockados para Testes

O FutPlay possui uma área administrativa para geração e limpeza de dados mockados.

Essa funcionalidade permite testar o sistema inteiro sem depender da API externa.

A área de dados de teste permite:

- Gerar campeonato de teste
- Gerar times mockados
- Gerar jogos finalizados e agendados
- Gerar ligas públicas e privadas
- Gerar participantes
- Gerar palpites
- Calcular classificação
- Calcular ranking
- Limpar dados mockados

Exemplo de dados gerados:

```text
Copa FutPlay Mock
Brasil Mock
Argentina Mock
França Mock
Alemanha Mock
Espanha Mock
Itália Mock
Portugal Mock
Inglaterra Mock
```

---

## 🔐 Autenticação e Segurança

O sistema utiliza **ASP.NET Core Identity** para autenticação.

Recursos implementados:

- Cadastro de usuário
- Login
- Logout
- Proteção de ações administrativas
- Área pública sem necessidade de login
- Área logada para ligas, palpites e manutenção de dados

### Regra de acesso

Área pública:

- Home
- Campeonatos
- Times
- Jogos
- Classificação
- Portal do campeonato
- Ranking público

Área logada:

- Criar e editar campeonatos
- Criar e editar times
- Criar e editar jogos
- Criar ligas
- Palpitar
- Gerenciar participantes
- Gerenciar palpites
- Importar dados da API
- Sincronizar campeonatos
- Gerar dados mockados

---

## 🧱 Arquitetura do Projeto

O projeto foi organizado em camadas para facilitar manutenção e evolução.

Estrutura principal:

```text
Controllers
Data
Models
Models/Api
Services
ViewModels
Views
wwwroot
```

### Controllers

Responsáveis por receber as requisições, chamar os serviços e retornar as Views.

Principais controllers:

```text
HomeController
CampeonatosController
TimesController
JogosController
LigasController
LigaParticipantesController
PalpitesController
ImportacoesController
MockDataController
```

### Services

A camada de serviços concentra regras de negócio e integrações.

Principais serviços:

```text
FootballApiService
ImportacaoCampeonatoService
ImportacaoJogosService
CampeonatoSincronizacaoService
ClassificacaoService
PontuacaoService
MockDataService
```

Essa separação evita controllers muito grandes e melhora a manutenção do projeto.

### ViewModels

As ViewModels são usadas para montar telas com dados combinados de várias entidades.

Exemplos:

```text
DashboardViewModel
RankingLigaViewModel
PalpitarLigaViewModel
ClassificacaoCampeonatoViewModel
PortalCampeonatoViewModel
ApiLigaViewModel
```

---

## 🗄️ Banco de Dados

Banco utilizado:

- SQL Server

Principais tabelas do sistema:

```text
FutPlay_Campeonatos
FutPlay_Times
FutPlay_Jogos
FutPlay_Ligas
FutPlay_LigaParticipantes
FutPlay_Palpites
FutPlay_Classificacoes
AspNetUsers
AspNetRoles
AspNetUserRoles
AspNetUserClaims
AspNetUserLogins
AspNetUserTokens
AspNetRoleClaims
```

O projeto utiliza **Entity Framework Core Migrations** para versionamento da estrutura do banco.

---

## ⚙️ Configuração

O projeto utiliza:

```text
appsettings.json
appsettings.Development.json
```

O arquivo `appsettings.json` deve ficar sem credenciais sensíveis.

Exemplo seguro:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "ApiFootball": {
    "BaseUrl": "https://v3.football.api-sports.io",
    "ApiKey": ""
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

O arquivo `appsettings.Development.json` deve conter as credenciais locais e **não deve ser enviado ao GitHub**.

Exemplo:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SERVIDOR;Database=BANCO;User Id=USUARIO;Password=SENHA;TrustServerCertificate=True;"
  },
  "ApiFootball": {
    "BaseUrl": "https://v3.football.api-sports.io",
    "ApiKey": "SUA_CHAVE_DA_API"
  }
}
```

---

## ▶️ Como Executar o Projeto

### 1. Clonar o repositório

```bash
git clone https://github.com/tiaorj/FutPlay.git
```

### 2. Abrir no Visual Studio 2022

Abra a solução no Visual Studio 2022.

### 3. Configurar o banco

Configure a connection string no arquivo:

```text
appsettings.Development.json
```

### 4. Restaurar pacotes NuGet

No Visual Studio:

```text
Compilar > Restaurar Pacotes NuGet
```

Ou pelo terminal:

```bash
dotnet restore
```

### 5. Executar migrations

No Console do Gerenciador de Pacotes:

```powershell
Update-Database
```

Ou pelo terminal:

```bash
dotnet ef database update
```

### 6. Rodar o projeto

Pressione:

```text
F5
```

ou execute:

```bash
dotnet run
```

---

## 🧭 Fluxo de Uso

### Fluxo público

```text
Acessar Home
Ver campeonatos
Abrir portal do campeonato
Ver jogos
Ver classificação
Ver rankings públicos
```

### Fluxo logado

```text
Criar conta
Fazer login
Criar liga
Cadastrar participantes
Palpitar nos jogos
Atualizar resultados
Recalcular pontuação
Ver ranking
```

### Fluxo com API

```text
Acessar Importações
Buscar campeonato
Importar campeonato
Importar jogos
Atualizar resultados
Sincronizar campeonato
```

### Fluxo com dados mockados

```text
Acessar Dados de Teste
Gerar dados mockados
Testar campeonato, jogos, classificação, ligas e ranking
Limpar dados mockados
Gerar novamente
```

---

## 📸 Telas Principais

Sugestões de telas para documentação futura:

- Home pública
- Portal do campeonato
- Classificação
- Jogos
- Ligas
- Palpitar por liga
- Ranking da liga
- Importações
- Dados de teste
- Login
- Cadastro de usuário

---

## 📚 Aprendizados do Projeto

Este projeto envolve conceitos importantes de desenvolvimento web moderno com .NET:

- ASP.NET Core MVC
- Entity Framework Core
- SQL Server
- Identity
- Autenticação
- Autorização por actions
- Consumo de API externa
- Separação de responsabilidades
- Camada de Services
- ViewModels
- Migrations
- Regras de negócio
- Integração com dados reais e mockados
- Layout responsivo com Bootstrap
- Organização de projeto para portfólio

---

## 🧭 Próximas Melhorias

Possíveis evoluções futuras:

- Criar perfis de acesso: Administrador e Participante
- Criar área “Minhas Ligas”
- Relacionar usuário logado com participante
- Criar painel administrativo separado
- Criar histórico de sincronizações
- Criar cache para reduzir chamadas na API
- Criar alertas de limite da API
- Criar importação de classificação direta da API
- Criar gráficos de desempenho dos participantes
- Criar tela de estatísticas dos palpites
- Criar geração de resumo da rodada com IA
- Criar análise de jogos com IA
- Criar notificações de jogos e palpites
- Publicar o projeto em ambiente online

---

## 🤖 Possíveis Usos de IA no FutPlay

Ideias futuras para uso de Inteligência Artificial:

- Gerar resumo automático da rodada
- Gerar análise pré-jogo
- Gerar análise pós-jogo
- Explicar mudanças na classificação
- Sugerir jogos importantes da rodada
- Criar textos automáticos para posts em redes sociais
- Criar insights sobre desempenho dos participantes
- Gerar previsões com base em estatísticas importadas

---

## 👨‍💻 Autor

Desenvolvido por **Sebastião Oliveira**.

Projeto criado para estudo, prática e portfólio profissional com foco em:

- Desenvolvimento web
- ASP.NET Core MVC
- Integração de sistemas
- SQL Server
- APIs externas
- Modernização de aplicações
- Arquitetura em camadas