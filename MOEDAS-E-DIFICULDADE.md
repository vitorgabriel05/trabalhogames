# Torre com moedas e dificuldade progressiva

Restaurada a base do backup `../leproso-backups/antes-aventura-20261004-160500.zip`. O experimento foi arquivado em `../leproso-backups/experimento-retirado-20261004`; o estado anterior a esta altera??o foi preservado em `../leproso-backups/antes-moedas-20261004.zip`.

Abra `TESTAR-MOEDAS.cmd` para jogar a vers?o Windows com o menu original. No Unity, abra `Assets/Project/Scenes/TorredasPlataformas.unity` ou use `Tools > Moedas > Abrir torre com moedas`.

- 19 moedas em bordas e desvios laterais, distribu?das pela torre.
- 16 quadros da sprite fornecida, em ordem de leitura, a 12 quadros por segundo. Fundo da folha removido e desenho da moeda preservado; todos os quadros t?m a mesma ?rea de 64 ? 90 pixels.
- Contador por tentativa junto de altura e recorde, com ?cone da moeda. Reiniciar a fase zera a coleta.
- Som de coleta respeita o volume dos efeitos. Apenas o jogador vivo coleta; cada moeda incrementa uma vez.
- Cinco apoios opcionais azuis oscilam verticalmente; as moedas acompanham seus movimentos.
- Cinco OVNIs adicionais guardam desvios de coleta.
- Patrulhas ficam mais r?pidas com a altura (per?odo de 5,8 a 3,2 segundos); plataformas inst?veis reduzem o aviso de 1,1 a 0,55 segundo; espetos t?m janelas seguras de 2,8 a 1,25 segundo e avisos de 0,75 a 0,5 segundo. Os ciclos s?o desencontrados.
- Os desvios s?o opcionais; as plataformas originais da rota principal foram preservadas.

A configura??o est? gravada na cena. `CoinClimbSetup.Apply` recria os desafios de forma idempotente. `CoinClimbValidation.Run` valida em Play Mode contador, recursos, coleta ?nica, anima??o e pausa e gera um build com menu e torre. A c?pia de valida??o e os logs est?o em `../trabalhogames-validacao-moedas`.

A valida??o automatizada confirma funcionamento. O equil?brio dos saltos e a dificuldade dos desvios ainda precisam de avalia??o jogando.
