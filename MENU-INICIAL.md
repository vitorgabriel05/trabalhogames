# Tela inicial e menu

Abra `Assets/Project/Scenes/Menu.unity` e pressione Play. Essa cena também é a primeira cena na configuração de build. A cena da partida continua disponível para testar diretamente pelo editor.

O vídeo enviado, com aproximadamente 10 segundos, toca em loop na primeira tela. Seu texto “APERTE ENTER” é preservado. Enter, Espaço ou clique abre o painel enviado pelo usuário sobre o vídeo escurecido. O vídeo inteiro é ajustado proporcionalmente à janela, com margens quando a proporção da janela difere de 16:9. No editor, a prévia Game View volta ao zoom que mostra o quadro inteiro ao entrar no menu.

- **Começar:** carrega a Torre das Plataformas.
- **Recorde:** usa `Assets/Resources/UI/Record.png`, a arte enviada, com o recorde de altura salvo pela partida desenhado em metros. O exemplo “118 m” da imagem é coberto com uma seção de madeira da própria arte; o número exibido é sempre o valor salvo. Enter/Escape e o botão X voltam ao menu.
- **Opções:** abre a arte enviada com volumes independentes de música e sons.
- **Voltar e X:** retornam à tela inicial. O X das opções volta ao menu.

Mouse, setas ou WASD selecionam os botões; Enter/Espaço confirmam; Escape volta. Nas opções, + e − mudam o volume em 10 pontos percentuais; as barras podem ser clicadas ou arrastadas. Setas esquerda/direita também ajustam a linha selecionada. Os valores são salvos em PlayerPrefs e continuam valendo ao iniciar a partida, reiniciar e abrir o jogo novamente. Música zero silencia as trilhas, incluindo as duas trilhas de game over; sons zero silencia efeitos, falas e interface.

A seleção tem elevação animada, sombra e halo verde-dourado, respeitando a transparência da arte original. A composição original `Assets/Resources/Audio/TitleTheme.wav` dura 71,11 segundos, a 108 BPM: flauta suave, sinos, arpejos, baixo e percussão discreta. São quatro seções com variação da melodia e caudas circulares de reverberação na emenda. `TitleSelect.wav` e `TitleConfirm.wav` são efeitos curtos originais de seleção e confirmação. `Tools/create_title_audio.py` recria esses três arquivos com Python e numpy.

`GameAudioSettings` mantém os ganhos independentes ao trocar de cena e registra fontes de áudio criadas durante o jogo. As fontes musicais são registradas explicitamente e a faixa existente `MusicManager.mp3` é reconhecida como música; as demais são consideradas sons. Para adicionar outra música futuramente, chame `GameAudioSettings.Register(source, true)` depois de configurar seu volume base.

Durante a tela inicial e todos os painéis do menu, os objetos da partida são desativados, o tempo da simulação fica em zero e o áudio externo ao menu é silenciado. Isso também vale ao entrar em Play com as cenas do menu e da partida abertas juntas. PlayerController e PauseScreen recusam ações enquanto o menu está ativo. Apenas as duas fontes do próprio menu podem tocar. O vídeo usa tempo independente da simulação. Começar encerra o vídeo e a música inicial, libera tempo/áudio e carrega a partida em modo Single, descartando as cenas antigas.

`MainMenuValidation.Run` reproduz em Play Mode as cenas do jogo e menu carregadas juntas e testa isolamento de objetos, bloqueio da pausa, fontes externas silenciadas, arte do recorde, ordem das cenas, importação, reprodução real do vídeo com simulação parada, navegação, volumes independentes, mute e recuperação do volume, persistência e início da partida. A validação deve ser executada em uma cópia do projeto, pois fecha o editor ao terminar. Log: `Logs/main-menu-validation.log`.
