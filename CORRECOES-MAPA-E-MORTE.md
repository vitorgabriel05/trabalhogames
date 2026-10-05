# Correções do mapa e animação de morte

Cena: `Assets/Project/Scenes/TorredasPlataformas.unity`.

- O chão inicial usa a mesma grama e terra, estendidas nas laterais (células -16 a 15), preenchendo os espaços que apareceram após remover as bordas.
- Os três conjuntos de espinhos iniciais voltaram às plataformas das células 12 e 18. A hierarquia anterior aplicava o deslocamento duas vezes. O conjunto retrátil também foi alinhado à plataforma da célula 43.
- Os dez trampolins estão apoiados na parte visível do terreno. Três que estavam fora da área jogável foram distribuídos entre as plataformas superiores. A colisão agora acompanha a área opaca do sprite, evitando superfícies invisíveis.
- A morte agora usa a referência de Undertale indicada pelo usuário. `PlayerDeathEffect` dissolve a pose atual em um coração vermelho sobre fundo preto, abre uma rachadura, mantém a pausa e espalha seis fragmentos que desaparecem gradualmente. Dura 3,8 segundos; controles, colisão e câmera param durante a reação. Colisões repetidas não reiniciam a animação. A queda pela borda inferior também mostra a reação antes de reiniciar. O HUD desaparece e os outros sons param durante a sequência.
- `Assets/Resources/Audio/UndertaleDeath.wav` contém o áudio extraído dos primeiros 2,25 segundos do vídeo enviado, sincronizado com a rachadura e o estilhaçamento. Origem e conversão estão registradas no arquivo de créditos ao lado do áudio.
- A altura e o recorde não recebem o deslocamento visual da animação de morte.

Saia do Play Mode e reabra a cena principal para carregar a versão salva no disco.

`Tools > Validar correcoes do mapa e morte` verifica terreno, apoio dos espinhos e trampolins, e o áudio da morte. `MapPolishValidation.Run`, em batchmode sem `-quit`, também captura o mapa e testa os dez trampolins, morte por espinhos, coração rachado, fragmentos, desaparecimento, câmera parada, repetição de colisões e morte por queda.

Verificações realizadas com Unity 6000.5.8f1: testes em Play Mode aprovados (`FINISH 0`) e build Windows aprovado. A sequência da alma, áudio importado, rachadura, seis fragmentos, desaparecimento, câmera parada, cobertura do fundo preto em tela larga e reinício por espinhos e queda foram verificados. Capturas e logs atuais em `Builds/UndertaleDeath` (`play-validation.log` e `build-validation.log`). A cópia de validação e o executável ficam em `../trabalhogames-validacao-espacial/Builds/Integration/Torre.exe`.

O indexador interno `UnityEditor.Search.SearchDatabase` repetiu a exceção já documentada em `INTEGRACAO.md`; os testes do jogo concluíram normalmente.
