# Pânico no Display — Corrida Infinita

Minijogo de corrida infinita feito em Unity 6. Um discente derrama café no servidor do laboratório
e agora só resta correr: pule e deslize para desviar dos obstáculos, a corrida fica cada vez mais
rápida e os 5 melhores scores ficam salvos no ranking.

## Controles

| Tecla | Ação |
|---|---|
| ESPAÇO ou ↑ | Pular |
| ↓ (segurar) | Deslizar por baixo dos fios |
| P | Pausar |
| M | Ligar / desligar o som |
| ENTER | Salvar o nome no TOP 5 |
| ESC | Pular o nome / voltar ao menu |

## Abrir no Unity

1. Instale o **Unity 6** (o projeto foi criado na versão `6000.6.8f1`) pelo Unity Hub.
2. No Unity Hub: **Add → Add project from disk** e escolha a pasta deste repositório
   (a que contém `Assets`, `Packages` e `ProjectSettings`).
3. Abra o projeto e aperte **Play**: o jogo abre sozinho.

## Gerar a versão para instalar (Windows)

No Unity: **Tools → Pânico no Display → Gerar instalável (Windows)**.

O jogo pronto sai na pasta `Build/Windows`. Para instalar em outro computador, copie a pasta
`Windows` **inteira** (não só o `.exe`) e abra `PanicoNoDisplay.exe`. Não precisa instalar o Unity
no computador do evento.

## Ranking

- O TOP 5 fica em `panico_no_display_ranking.json`, na pasta de dados do jogo
  (`%USERPROFILE%\AppData\LocalLow\PanicoNoDisplay\Panico no Display`).
- Para zerar: apague esse arquivo, ou no Unity use **Tools → Pânico no Display → Zerar ranking (TOP 5)**.
- Na tela de fim, 20 segundos sem ninguém mexer voltam sozinhos para o menu.

## Estrutura

```
Assets/
  PanicoNoDisplay/     o minijogo (scripts, artes, sons) — pode ser copiado para outro projeto
    Scripts/           código do jogo
    Editor/            menus de Tools → Pânico no Display
    Resources/         artes, sons e shaders
    LEIAME.md          detalhes técnicos e ajustes de dificuldade
  Standalone/          faz este projeto abrir o minijogo sozinho e gera o instalável
  Scenes/              cena vazia de entrada
  Settings/            configurações do URP 2D
  TextMesh Pro/        recursos do TextMeshPro
Packages/              pacotes do Unity
ProjectSettings/       configurações do projeto
```

A dificuldade (velocidade, espaço entre obstáculos, combos) é ajustada nas constantes no topo de
`Assets/PanicoNoDisplay/Scripts/PNDLevel.cs`.
