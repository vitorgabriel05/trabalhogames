# Continuação da torre e retorno pelas moedas

A cena `Assets/Project/Scenes/TorredasPlataformas.unity` mantém o trecho original e recebe desafios entre as coordenadas Y 125,8 e 295,4. A altura no HUD desconta a posição inicial do jogador.

- Precisão: apoios móveis com ciclos desencontrados e apoios fixos.
- Ritmo: plataformas mais largas com espetos retráteis na lateral, luz de aviso e espaço para esperar.
- Fuga espacial: plataformas instáveis que retornam, patrulhas atravessando alguns saltos e OVNIs nos desvios.
- Desafio final: combinação das três mecânicas, terminando em um apoio permanente com bandeira.

São 54 apoios de rota, 13 desvios opcionais alternando os lados, 29 moedas novas, 10 conjuntos retráteis e 7 OVNIs novos. Há apoios verdes de descanso aproximadamente a cada seis saltos. Os novos saltos sobem 3,2 unidades, com deslocamentos horizontais de três unidades. Os desvios dourados ficam fora da rota principal; coletar moedas continua opcional. A coleta permanece por tentativa, sem loja ou saldo persistente.

A câmera acompanha a subida e a descida sem ficar presa ao pico. Durante uma queda, olha um pouco para baixo e limita o atraso para manter o personagem visível. O recorde de altura continua preservado. Sair pela borda inferior da tela não mata: a queda só é fatal quando o corpo inteiro passa do limite fixo, 12 unidades abaixo do início da fase. A colisão contínua do jogador evita atravessar apoios finos em descidas rápidas. Espinhos e serras continuam letais. Os componentes antigos `PlayerMorte` e `GameManager` também deixam de usar a câmera como limite de morte.

Para jogar no Windows, abra `TESTAR-MOEDAS.cmd`. No Unity, abra a cena principal. `Tools > Torre > Aplicar continuacao ate o topo` recria somente o grupo `Continuacao da torre`, sem acumular cópias.

`UpperTowerSetup.Run` aplica a continuação e executa `UpperTowerValidation.Run` em Unity batchmode, sem `-quit`. O teste verifica os saltos com física 2D e os valores atuais de pulo, descida de 90 unidades, visibilidade, coleta abaixo do pico, pausa, morte abaixo da base, reinício, retorno ao título e colisão letal de um OVNI novo. Na verificação dos saltos, as patrulhas e os espetos ficam desativados e os apoios móveis ficam parados; o teste confirma alcance e colisão das plataformas, mas o equilíbrio dos ciclos exige avaliação jogando.

O processo gera o executável `Builds/CoinClimb/Leproso.exe` e capturas nas alturas 127, 170, 212, 256 e 295. Log: `Logs/upper-validation.log`.
