# Obst?culos e progress?o

Cena principal: `Assets/Project/Scenes/TorredasPlataformas.unity`.

O in?cio mant?m as plataformas, serras e espinhos originais. A plataforma da c?lula 22 foi restaurada ao tilemap. Os OVNIs e seus pilotos foram removidos das tr?s serras anteriores ao espa?o.

A progress?o usa a posi??o no mapa, sem depender do recorde salvo:

- Plataforma inst?vel inicial na c?lula 37, com aviso de 1,4 segundos.
- Espetos retr?teis no conjunto da altura 44; os tr?s conjuntos iniciais mant?m o comportamento original.
- OVNIs a partir da serra na altura 61,58, dentro do trecho espacial. Os ciclos come?am perto de 5,8 segundos e ficam mais r?pidos na subida.
- Cinco plataformas inst?veis ao longo do mapa, com avisos gradualmente menores, at? 0,75 segundo.

Os valores acima s?o coordenadas do mapa; a altura mostrada no HUD desconta a posi??o inicial do jogador.

Os fundos usam `Game/ScrollingBackground`, que aplica o deslocamento UV em todas as se??es. As se??es cobrem a c?mera inteira e se misturam gradualmente em uma faixa de quatro unidades ao redor de cada mudan?a de mapa. O desenho mant?m o deslocamento vertical durante a subida, e as bordas da textura s?o espelhadas para evitar linhas e esticamento. Os fundos ficam atr?s dos obst?culos e se adaptam ? largura da tela. A c?mera limpa a tela a cada quadro, evitando res?duos nas laterais.

`Tools > Obstaculos espaciais > Validar cena` verifica 7 OVNIs, 5 plataformas e 1 conjunto retr?til, al?m dos limites do trecho inicial e dos materiais de fundo.

`SpaceObstacleValidation.ReviewAndBuild` valida a integra??o, gera `Builds/Integration/Torre.exe`, captura as alturas 20, 40, 56, 60 e 103 e testa o movimento de todos os fundos, a cobertura da tela, a queda e o retorno das plataformas e as colis?es das armadilhas. Execute em uma c?pia do projeto com Unity em batchmode, sem `-quit`.

O comando `SpaceObstacleSetup.Apply` cria apenas os obst?culos avan?ados em uma cena anterior ? altera??o. A cena principal j? est? configurada.

Valida??o desta corre??o: compila??o, build Windows e testes em Play Mode aprovados (`FINISH 0`). As capturas confirmam a aus?ncia das bordas amarelas e da linha entre fundos. Log local: `Builds/SpaceObstacles/transicao-validacao-final.log`; captura da transi??o: `Builds/SpaceObstacles/transicao-validada-56.png`. O indexador interno UnityEditor.Search repetiu a exce??o j? documentada em INTEGRACAO.md; os testes conclu?ram normalmente.
