# Pausa e continuação da música de morte

Escape ou P abre/fecha a pausa. Use mouse, setas ou WASD para escolher; Enter ou Espaço confirma. BORA retoma a partida, reiniciar recarrega a cena, reiniciar pontuação apaga o recorde de altura desta cena e começa outra tentativa, e dar o fora retorna à tela inicial do jogo, inclusive no Play Mode do editor.

`PauseScreen` é adicionado automaticamente ao jogador. A imagem original de Downloads foi copiada para `Assets/Resources/UI/Pause.png`; o menu desenha apenas seu painel central usando coordenadas de textura, preservando o arquivo original. O cenário da partida fica congelado, desfocado e escurecido atrás do painel. A seleção eleva o botão e acrescenta halo, luz superior e sombra inferior. Os efeitos usam tempo real e continuam funcionando durante a pausa.

O áudio do jogo é suspenso com `AudioListener.pause` e volta do mesmo ponto ao retomar. Seleção e confirmação usam canais que continuam tocando durante a pausa; a confirmação espera 0,16 segundo antes de reiniciar ou sair. Os estados anteriores de áudio e velocidade do jogo são restaurados.

Na morte, `GameOverMusic.mp3` toca uma única vez. A continuação `GameOverAmbient.wav` começa no final exato da introdução, agendada pelo relógio DSP de áudio. A composição dura 96 segundos, com acordes suaves, frases espaçadas e variações. Dó e mi do final da introdução orientam a harmonia inicial. As caudas de reverberação atravessam a emenda do loop; o arquivo é importado sem compressão para preservar a emenda. `Tools/create_menu_audio.py` reproduz os três novos arquivos WAV e requer numpy.

Validação automatizada em Play Mode: `PauseValidation.Run` verifica importação da arte e sons, congelamento, restauração de áudio/tempo, BORA, reinício e limpeza do recorde usando uma chave temporária. `GameOverValidation.Run` verifica introdução sem loop, transição para a continuação com o jogo congelado, falas e reinício. Logs: `Logs/pause-validation.log` e `Logs/pause-death-validation.log`. O teste batch não verifica visualmente o desfoque e os brilhos.

Esta configuração substitui a descrição anterior de música curta em loop em `TELA-GAME-OVER.md`.
