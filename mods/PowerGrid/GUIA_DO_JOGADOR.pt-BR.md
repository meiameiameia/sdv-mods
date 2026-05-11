# Guia do Jogador - PowerGrid

Este é o guia prático rápido para usar PowerGrid sem precisar aprender tudo no susto.

PowerGrid adiciona geradores, baterias, cabos, conduítes, itens de combustível, máquinas artesanais energizadas e uma pequena tier industrial de processamento. Máquinas energizadas não duplicam itens nem ignoram insumos. Elas apenas trabalham mais rápido quando a rede consegue fornecer EU suficiente.

## Primeiro Setup

Comece pequeno:

1. Coloque um `Gerador a Vapor`.
2. Abasteça com `Carvão`, `Madeira` ou `Madeira de Lei`.
3. Coloque `Cabo de Cobre` ao lado do gerador.
4. Coloque uma máquina energizada ao lado do cabo ou ao lado de outra máquina energizada.
5. Adicione uma `Bateria Básica de Energia` quando puder.
6. Abra a Aba de Energia com `P` ou `K` para inspecionar a rede.

Máquinas e objetos do PowerGrid se conectam para cima, baixo, esquerda e direita. Diagonal não conecta.

Máquinas energizadas podem passar energia para outras máquinas energizadas encostadas nelas, então você não precisa colocar cabo entre todas as máquinas. Use os cabos como linhas principais.

## Regras Importantes

- Geradores com combustível produzem o EU completo enquanto estão ligados.
- EU sobrando é armazenado em baterias se a rede tiver espaço.
- Se não houver espaço em bateria, o EU sobrando é desperdiçado.
- Baterias ajudam a suavizar picos de demanda e reduzem desperdício.
- Geradores Eólicos só produzem energia ao ar livre.
- A maioria das máquinas energizadas ainda funciona normalmente sem energia.
- Energia é um bônus de velocidade, não um substituto para o funcionamento normal das máquinas.
- A principal exceção é a `Fornalha Elétrica`: ela precisa de uma rede energizada ativa para começar a fundir.

## Desbloqueios

| Pacote | Condição de Desbloqueio | Receitas |
| --- | --- | --- |
| Início da Rede | Mineração 5 ou conhecer Para-raios | Cabo de Cobre, Gerador a Vapor, Bateria Básica de Energia |
| Artesanato Energizado | Conhecer Jarra de Conservas ou Barril | Jarra de Conservas Industrial, Barril de Metal |
| Tecnologia de Combustível | Mineração 7 e conhecer Para-raios | Biocombustível, Cabo de Ferro, Gerador de Combustão, Fornalha Elétrica, Bobina de Aquecimento, Núcleo de Eficiência, Câmara Catalítica, Recicladora Industrial, Ímã de Separação, Desidratador Energizado, Conjunto de Prateleiras de Secagem, Regulador de Calor |
| Rede Avançada | Mineração 9 e conhecer Para-raios | Cabo de Irídio, Gerador Eólico, Bateria de Energia de Irídio, Conduíte de Energia, Barril de Envelhecimento de Metal, Barril de Irídio Reforçado |
| Rede de Alta Densidade | Mineração 10, conhecer Para-raios, conhecer Painel Solar e conhecer Bateria de Energia de Irídio | Cabo de Irídio Energizado, Combustível Radioisotópico, Gerador Radioisotópico |

Se você usa Generic Mod Config Menu, o comportamento dos desbloqueios pode ser ajustado dentro do jogo.

## Geradores

| Gerador | Produção | Combustível | Observações |
| --- | ---: | --- | --- |
| Gerador a Vapor | 75 EU/tick | Carvão, Madeira, Madeira de Lei | Gerador inicial. Bom para salas pequenas. |
| Gerador de Combustão | 240 EU/tick | Biocombustível | Gerador de meio de jogo. Bom para grupos maiores de máquinas. |
| Gerador Radioisotópico | 900 EU/tick | Combustível Radioisotópico, Barra Radioativa | Gerador de alta densidade para o fim de jogo e salas de produção pesadas. |
| Gerador Eólico | 25 EU/tick base | nenhum | Energia passiva, apenas ao ar livre, ajustada pelo clima. |

Receitas de geradores:

| Gerador | Receita |
| --- | --- |
| Gerador a Vapor | Barra de Ferro x5, Barra de Cobre x2, Carvão x6, Quartzo Refinado x1 |
| Gerador de Combustão | Gerador a Vapor x1, Barra de Ferro x8, Barra de Ouro x5, Quartzo Refinado x3 |
| Gerador Radioisotópico | Gerador de Combustão x1, Barra Radioativa x3, Barra de Irídio x6, Bateria de Energia de Irídio x1, Quartzo Refinado x4 |
| Gerador Eólico | Barra de Ferro x6, Quartzo Refinado x4, Bateria x1, Carvão x4 |

A produção eólica muda com o clima:

- Tempo limpo: produção normal
- Chuva ou tempestade: produção maior
- Neve: produção menor

Clique com o botão direito em um `Gerador a Vapor` segurando `Carvão`, `Madeira` ou `Madeira de Lei` para abastecer. Clique com o botão direito em um `Gerador de Combustão` segurando `Biocombustível` para abastecer. Clique com o botão direito em um `Gerador Radioisotópico` segurando `Combustível Radioisotópico` ou uma `Barra Radioativa` para abastecer.

## Combustível

| Combustível | Usado Por | Observações |
| --- | --- | --- |
| Carvão | Gerador a Vapor | Melhor combustível do Gerador a Vapor. |
| Madeira | Gerador a Vapor | Combustível reserva fácil de conseguir. |
| Madeira de Lei | Gerador a Vapor | Melhor que Madeira. |
| Biocombustível | Gerador de Combustão | Combustível criado no meio do jogo. |
| Combustível Radioisotópico | Gerador Radioisotópico | Combustível fabricado de fim de jogo. Muito mais eficiente que barras em bruto. |
| Barra Radioativa | Gerador Radioisotópico | Combustível em bruto legado. Menos eficiente que Combustível Radioisotópico. |

Receita do Biocombustível:

| Receita | Resultado |
| --- | ---: |
| Fibra x10, Madeira x5, Carvão x1 | Biocombustível x8 |

Receita do Combustível Radioisotópico:

| Receita | Resultado |
| --- | ---: |
| Barra Radioativa x1, Quartzo Refinado x1 | Combustível Radioisotópico x7 |

## Cabos

| Cabo | Receita | Capacidade |
| --- | --- | ---: |
| Cabo de Cobre | Barra de Cobre x3 | 10 cabos, 50 EU/tick |
| Cabo de Ferro | Barra de Ferro x3 | 10 cabos, 250 EU/tick |
| Cabo de Irídio | Barra de Irídio x2, Quartzo Refinado x1 | 10 cabos, 1.000 EU/tick |
| Cabo de Irídio Energizado | Barra de Irídio x4, Barra Radioativa x1, Bateria x1, Quartzo Refinado x2 | 10 cabos, 3.000 EU/tick |

Capacidade é quanto EU uma rede consegue mover por tick. Se uma rede mistura cabos de tiers diferentes, o cabo mais fraco limita a rede.

## Baterias

| Bateria | Receita | Capacidade |
| --- | --- | ---: |
| Bateria Básica de Energia | Bateria x1, Barra de Cobre x4, Quartzo Refinado x1 | 500 EU |
| Bateria de Energia de Irídio | Bateria x2, Barra de Irídio x2, Quartzo Refinado x3 | 2.000 EU |

Baterias valem a pena cedo. Elas guardam EU sobrando, cobrem picos de demanda e mantêm máquinas energizadas quando os geradores ficam sem combustível ou quando a produção eólica cai.

O EU armazenado fica no próprio item da bateria quando você a pega e coloca em outro lugar.

## Conduítes de Energia

Conduítes de Energia conectam redes entre locais, como Fazenda para Galpão ou Fazenda para Casa da Fazenda.

| Item | Receita |
| --- | --- |
| Conduíte de Energia | Barra de Irídio x1, Bateria x1, Quartzo Refinado x1 |

Para ligar conduítes:

1. Coloque um conduíte em cada local.
2. Conecte cada conduíte à sua rede local.
3. Abra a Aba de Energia e selecione dois conduítes para ligar.

Você também pode clicar com o botão direito em um conduíte e depois clicar com o botão direito no outro. `Shift + clique direito` cancela pareamento ou remove a ligação de um conduíte.

## Máquinas Artesanais Energizadas

| Máquina | Receita | Uso de Energia | Bônus Máximo |
| --- | --- | ---: | ---: |
| Jarra de Conservas Industrial | Madeira x30, Carvão x4, Barra de Ferro x4, Quartzo Refinado x1 | 20 EU/tick | 20% mais rápida |
| Barril de Metal | Barra de Ferro x6, Barra de Cobre x4, Quartzo Refinado x1 | 10 EU/tick | 20% mais rápido |
| Barril de Irídio Reforçado | Barra de Irídio x4, Barra de Ferro x2, Quartzo Refinado x1 | 30 EU/tick | 30% mais rápido |
| Barril de Envelhecimento de Metal | Madeira de Lei x8, Barra de Ferro x6, Barra de Irídio x2, Quartzo Refinado x1 | 40 EU/tick | 50% mais rápido para envelhecer |

O Barril de Irídio Reforçado foi pensado para usar o comportamento do Hardwood Keg de Grapes of Ferngill. Sem Grapes of Ferngill, ele usa o comportamento do Barril vanilla.

## Máquinas Industriais de Processamento da 0.3

### Fornalha Elétrica

| Máquina | Receita | Uso de Energia | Bônus Máximo |
| --- | --- | ---: | ---: |
| Fornalha Elétrica | Barra de Ferro x8, Barra de Ouro x4, Quartzo Refinado x3, Bateria x1 | 40 EU/tick | 50% mais rápida |

O que ela faz:

- Funde minério em barras.
- Precisa de uma rede energizada ativa antes de começar.
- Se perder a conexão de energia no meio do processo, o minério é devolvido em vez de terminar de graça.

Receitas-base suportadas:

| Entrada | Saída | Tempo Base |
| --- | --- | ---: |
| Minério de Cobre x5 | Barra de Cobre x1 | 30m |
| Minério de Ferro x5 | Barra de Ferro x1 | 120m |
| Minério de Ouro x5 | Barra de Ouro x1 | 300m |
| Minério de Irídio x5 | Barra de Irídio x1 | 480m |
| Minério Radioativo x5 | Barra Radioativa x1 | 600m |

Chance base de barra extra: 5%

### Recicladora Industrial

| Máquina | Receita | Uso de Energia | Bônus Máximo |
| --- | --- | ---: | ---: |
| Recicladora Industrial | Barra de Ferro x6, Quartzo Refinado x4, Barra de Cobre x4, Carvão x4 | 20 EU/tick | 35% mais rápida |

O que ela faz:

- Recicla lixo suportado em materiais úteis de infraestrutura.
- Ainda funciona sem energia, mas energia acelera o processo.
- O tempo base do processo é 60m.

Saídas-base suportadas:

| Entrada | Saída |
| --- | --- |
| Lixo | Pedra x2 |
| Madeira à Deriva | Madeira x2 |
| Óculos Quebrados | Quartzo Refinado x1 |
| CD Quebrado | Quartzo Refinado x1 |
| Jornal Encharcado | Fibra x3 |
| Refrigerante Joja | Carvão x1 |

### Desidratador Energizado

| Máquina | Receita | Uso de Energia | Bônus Máximo |
| --- | --- | ---: | ---: |
| Desidratador Energizado | Barra de Ferro x6, Madeira de Lei x10, Quartzo Refinado x3, Bateria x1 | 20 EU/tick | 40% mais rápido |

O que ele faz:

- Processa frutas e cogumelos como o Desidratador vanilla.
- Ainda funciona sem energia, mas energia acelera o processo.

## Painel da Máquina e Melhorias

Abra o painel da máquina com `Shift + clique direito` em:

- `Fornalha Elétrica`
- `Recicladora Industrial`
- `Desidratador Energizado`

Como usar:

1. Segure uma melhoria compatível.
2. Abra o painel com `Shift + clique direito`.
3. Instale a melhoria pelo painel.
4. Use o mesmo painel depois se quiser remover a melhoria.

Cada nova máquina industrial atualmente tem um slot de melhoria.

### Receitas de Melhorias

| Melhoria | Receita |
| --- | --- |
| Bobina de Aquecimento | Barra de Ouro x3, Quartzo Refinado x2, Carvão x6 |
| Núcleo de Eficiência | Quartzo Refinado x4, Bateria x1, Barra de Ouro x2 |
| Câmara Catalítica | Barra de Irídio x2, Barra de Ouro x4, Quartzo Refinado x4 |
| Ímã de Separação | Barra de Ferro x4, Quartzo Refinado x2 |
| Conjunto de Prateleiras de Secagem | Madeira x20, Madeira de Lei x4, Barra de Ouro x1 |
| Regulador de Calor | Barra de Ouro x2, Quartzo Refinado x2, Carvão x4 |

### Efeitos das Melhorias

| Melhoria | Máquina | Efeito |
| --- | --- | --- |
| Bobina de Aquecimento | Fornalha Elétrica | Reduz o tempo de fundição e aumenta a demanda de EU. |
| Núcleo de Eficiência | Fornalha / Recicladora / Desidratador | Reduz a demanda de EU. |
| Câmara Catalítica | Fornalha Elétrica | Aumenta a chance de barra extra e aumenta a demanda de EU. |
| Ímã de Separação | Recicladora Industrial | Adiciona melhores chances de recuperar metal e aumenta a demanda de EU. |
| Conjunto de Prateleiras de Secagem | Desidratador Energizado | Pode melhorar a quantidade produzida por lote e aumenta a demanda de EU. |
| Regulador de Calor | Desidratador Energizado | Reduz o tempo de desidratação e aumenta a demanda de EU. |

## Progressão Sugerida

Início:

- Gerador a Vapor
- Cabo de Cobre
- Bateria Básica de Energia
- algumas Jarras de Conservas Industriais ou Barris de Metal

Meio do jogo:

- Biocombustível
- Gerador de Combustão
- Cabo de Ferro
- Fornalha Elétrica
- Recicladora Industrial
- Desidratador Energizado
- primeiros itens de melhoria

Mais tarde:

- Cabo de Irídio
- Cabo de Irídio Energizado
- Gerador Radioisotópico
- Bateria de Energia de Irídio
- Conduítes de Energia
- Geradores Eólicos ao ar livre
- Barris de Envelhecimento de Metal e Barris de Irídio Reforçado
- ajustes de melhorias no fim do jogo

## Solução de Problemas

Se uma máquina não recebe energia:

- Veja se ela está conectada por cabo ou encostada em outra máquina energizada.
- Veja se o gerador está abastecido ou produzindo energia.
- Veja se a máquina está realmente processando algo se você espera um bônus de velocidade.
- Confira a Aba de Energia para ver rede, demanda, armazenamento e estado das máquinas.

Se a `Fornalha Elétrica` diz que precisa de energia:

- Ela não pode começar a fundir em uma rede morta ou desconectada.
- Confira primeiro geração ativa, combustível, baterias e adjacência dos cabos.

Se uma melhoria não instala:

- Segure o item de melhoria antes de abrir o painel da máquina.
- Veja se a melhoria é compatível com aquela máquina.
- Lembre que cada máquina atualmente tem um único slot de melhoria.

Se um `Gerador Eólico` está offline:

- Veja se ele está ao ar livre.

Se combustível parece estar sendo desperdiçado:

- Adicione baterias para guardar EU sobrando.

Se uma ligação de conduíte está errada:

- Use a aba Conduítes na Aba de Energia.
- `Shift + clique direito` em um conduíte remove a ligação ou cancela o pareamento.

## Mods Recomendados

PowerGrid funciona sozinho, mas fica melhor com:

- Grapes of Ferngill
- Automate ou Event Driven Automation
- Generic Mod Config Menu
