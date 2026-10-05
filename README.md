# Torre das Plataformas

## Como testar

1. Clone este repositorio e selecione a branch `feature/plataformas-e-obstaculos`:

   ```sh
   git clone --branch feature/plataformas-e-obstaculos https://github.com/vitorgabriel05/trabalhogames.git
   ```

2. No Unity Hub, adicione a pasta do projeto e abra com **Unity 6000.5.8f1**.
3. Aguarde a importacao dos assets e pacotes.
4. Abra `Assets/Project/Scenes/Menu.unity` e pressione **Play**.
5. Pressione Enter para abrir o menu e escolha **Comecar**.

O menu inclui recorde de altura e volumes independentes de musica e sons. A partida inclui moedas, plataformas, obstaculos espaciais, pausa e tela de game over.

Para atualizar uma copia existente, salve suas alteracoes locais antes de trocar de branch e executar `git pull`.

## Build Windows

As pastas `Builds`, `Library` e `Logs` sao locais e nao estao no repositorio. Para gerar um executavel, use a configuracao de build do Unity com `Menu` como primeira cena e `TorredasPlataformas` como segunda. Compartilhe a pasta completa do build, incluindo os arquivos ao lado do `.exe`.

## Detalhes das funcionalidades

- [Menu inicial](MENU-INICIAL.md)
- [Moedas e dificuldade](MOEDAS-E-DIFICULDADE.md)
- [Obstaculos espaciais](OBSTACULOS-ESPACIAIS.md)
- [Continuacao da torre](CONTINUACAO-DA-TORRE.md)
- [Pausa e musica](PAUSA-E-MUSICA.md)
- [Game over](TELA-GAME-OVER.md)
- [Correcoes de mapa e morte](CORRECOES-MAPA-E-MORTE.md)
