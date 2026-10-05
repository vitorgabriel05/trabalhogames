# Tela de game over

A imagem enviada está em `Assets/Resources/UI/GameOver.png`. Após os 3,8 segundos da animação de morte, o jogo fica pausado e mostra a ilustração completa, com o menu pixelado `CONTINUE? Sim / Não`, seta piscando e vagalumes com brilho pulsante e luzes em movimento.

Use as setas esquerda/direita ou A/D para escolher e Enter ou Espaço para confirmar. Também é possível clicar nas opções. Sim reinicia a cena; Não fecha o jogo (no editor, sai do Play Mode).

A transição dos mapas voltou ao comportamento anterior: fundos nas posições e escalas originais, materiais originais e apenas movimento horizontal, sem mistura dos mapas pela tela inteira.

Validação em Play Mode no Unity 6000.5.8f1 aprovada: recursos importados, fundos preservam posição e escala, menu espera a animação terminar, partida fica pausada, Sim começa selecionado e reinicia com jogador vivo e tempo normal. Log: `Logs/game-over-play.log`. Para repetir: executar `GameOverValidation.Run` em batchmode sem `-quit`. A captura de imagem não foi produzida pelo modo batch.

Este comportamento substitui o reinício automático descrito em `CORRECOES-MAPA-E-MORTE.md`.


## M?sica, falas e visual

A tela agora preenche toda a janela do jogo com a ilustra??o completa. O t?tulo GAME OVER tem brilho e halo pulsantes, animados mesmo com o jogo pausado. O menu CONTINUE? fica diretamente sobre a ilustra??o, sem o painel preto.

Ao abrir o game over, `GameOverScreen` toca `Assets/Resources/Audio/GameOverMusic.mp3` em loop e sorteia uma das seis falas `GameOverVoice1.mp3` a `GameOverVoice6.mp3` em um canal separado. A m?sica foi importada do link enviado: https://www.youtube.com/watch?v=keRzQHTYBMw . As falas seguem a ordem dos arquivos enviados (Dormiu, Aura, Parab?ns, Dif?cil, Cuidado, Morreu).

`DeathCount` conta as mortes durante a sess?o e continua ap?s reiniciar a cena; `SelectedVoiceIndex` informa a fala sorteada, de 0 a 5. O sorteio evita repetir a fala imediatamente anterior. Os volumes `musicVolume` (0,35) e `voiceVolume` (1) podem ser ajustados no script. Os dois canais param ao confirmar Sim ou N?o.

Valida??o atualizada: `GameOverValidation.Run` verifica a importa??o dos sete ?udios, reprodu??o da m?sica durante a pausa, sele??o da fala, contador preservado no rein?cio e um novo sorteio na segunda morte. Log: `Logs/game-over-audio-validation.log`.
