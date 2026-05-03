# Guia do Jogador - PowerGrid

Este guia é um resumo rápido para jogar com PowerGrid sem precisar descobrir tudo no susto.

PowerGrid adiciona geradores, baterias, cabos, conduítes, Biocombustível e máquinas artesanais energizadas. As máquinas energizadas não duplicam itens e não mudam o que elas produzem. Elas apenas trabalham mais rápido quando a rede tem EU suficiente.

## Primeiro Setup

Comece pequeno:

1. Coloque um Gerador a Vapor.
2. Abasteça com Carvão, Madeira ou Madeira de Lei.
3. Coloque Cabo de Cobre ao lado do gerador.
4. Coloque uma máquina energizada ao lado do cabo ou ao lado de outra máquina energizada.
5. Adicione uma Bateria de Energia Básica quando puder.
6. Abra a Aba de Energia com `P` ou `K` para ver a rede.

Máquinas e objetos do PowerGrid se conectam para cima, baixo, esquerda e direita. Diagonal não conecta.

Máquinas energizadas podem passar energia para outras máquinas energizadas encostadas nelas, então você não precisa colocar cabo entre todas as máquinas. Use os cabos como linhas principais.

## Regras Importantes

- Geradores com combustível produzem o EU completo enquanto estão ligados.
- EU sobrando é armazenado em baterias se a rede tiver espaço.
- Se não houver espaço em bateria, o EU sobrando é desperdiçado.
- Baterias ajudam a suavizar picos de demanda e reduzem desperdício.
- Geradores Eólicos só produzem energia ao ar livre.
- Máquinas energizadas ainda funcionam normalmente sem energia.
- Energia é um bônus de velocidade, não um substituto para o funcionamento normal das máquinas.

## Desbloqueios

| Pacote | Condição | Receitas |
| --- | --- | --- |
| Início da Rede | Mineração 5 ou conhecer Para-raios | Cabo de Cobre, Gerador a Vapor, Bateria de Energia Básica |
| Artesanato Energizado | Conhecer Jarra de Conserva ou Barril | Jarra de Conserva Industrial, Barril de Metal |
| Tecnologia de Combustível | Mineração 7 e conhecer Para-raios | Biocombustível, Cabo de Ferro, Gerador de Combustão |
| Rede Avançada | Mineração 9 e conhecer Para-raios | Cabo de Irídio, Gerador Eólico, Bateria de Energia de Irídio, Conduíte de Energia, Barril de Envelhecimento de Metal, Barril de Irídio Rígido |
| Rede de Alta Densidade | Mineração 10, conhecer Para-raios, conhecer Painel Solar e conhecer Bateria de Energia de Irídio | Cabo de Irídio Energizado, Gerador Radioisotópico |

Se você usa Generic Mod Config Menu, o comportamento dos desbloqueios pode ser ajustado dentro do jogo.

## Geradores

| Gerador | Produção | Combustível | Observações |
| --- | ---: | --- | --- |
| Gerador a Vapor | 75 EU/tick | Carvão, Madeira, Madeira de Lei | Gerador inicial. Bom para salas pequenas. |
| Gerador de Combustão | 240 EU/tick | Biocombustível | Gerador de meio de jogo. Bom para grupos maiores de máquinas. |
| Gerador Radioisotópico | 900 EU/tick | Barra Radioativa | Gerador de alta densidade para o fim de jogo e salas de produção pesadas. |
| Gerador Eólico | 25 EU/tick base | nenhum | Energia passiva, apenas ao ar livre, ajustada pelo clima. |

A produção eólica muda com o clima:

- Tempo limpo: produção normal
- Chuva ou tempestade: produção maior
- Neve: produção menor

Clique com o botão direito em um Gerador a Vapor segurando Carvão, Madeira ou Madeira de Lei para abastecer. Clique com o botão direito em um Gerador de Combustão segurando Biocombustível para abastecer. Clique com o botão direito em um Gerador Radioisotópico segurando uma Barra Radioativa para abastecer.

## Combustível

| Combustível | Usado por | Observações |
| --- | --- | --- |
| Carvão | Gerador a Vapor | Melhor combustível do Gerador a Vapor. |
| Madeira | Gerador a Vapor | Combustível reserva fácil de conseguir. |
| Madeira de Lei | Gerador a Vapor | Melhor que Madeira. |
| Biocombustível | Gerador de Combustão | Combustível criado no meio do jogo. |
| Barra Radioativa | Gerador Radioisotópico | Combustível denso de fim de jogo para produção muito alta de EU. |

Receita do Biocombustível:

| Receita | Resultado |
| --- | ---: |
| Fibra x10, Madeira x5, Carvão x1 | Biocombustível x8 |

## Cabos

| Cabo | Receita | Capacidade |
| --- | --- | ---: |
| Cabo de Cobre | Barra de Cobre x3 | 10 cabos, 50 EU/tick |
| Cabo de Ferro | Barra de Ferro x3 | 10 cabos, 250 EU/tick |
| Cabo de Irídio | Barra de Irídio x2, Quartzo Refinado x1 | 10 cabos, 1.000 EU/tick |
| Cabo de Irídio Energizado | Barra de Irídio x4, Barra Radioativa x1, Bateria x1, Quartzo Refinado x2 | 10 cabos, 3.000 EU/tick |

Capacidade é quanto EU uma rede consegue mover por tick. Se uma rede mistura cabos de tiers diferentes, o cabo mais fraco limita a rede.

## Baterias

| Bateria | Receita | Armazenamento |
| --- | --- | ---: |
| Bateria de Energia Básica | Bateria x1, Barra de Cobre x4, Quartzo Refinado x1 | 500 EU |
| Bateria de Energia de Irídio | Bateria x2, Barra de Irídio x2, Quartzo Refinado x3 | 2.000 EU |

Baterias valem a pena cedo. Elas guardam EU sobrando, cobrem picos de demanda e mantêm máquinas energizadas quando os geradores ficam sem combustível ou quando a produção eólica cai.

## Conduítes de Energia

Conduítes de Energia conectam redes entre locais, como Fazenda para Galpão ou Fazenda para Casa da Fazenda.

Para ligar conduítes:

1. Coloque um conduíte em cada local.
2. Conecte cada conduíte à sua rede local.
3. Abra a Aba de Energia e selecione dois conduítes para ligar.

Você também pode clicar com o botão direito em um conduíte e depois clicar com o botão direito no outro. Shift + clique direito cancela pareamento ou remove a ligação de um conduíte.

## Máquinas Energizadas

| Máquina | Receita | Uso de Energia | Bônus Máximo |
| --- | --- | ---: | ---: |
| Jarra de Conserva Industrial | Madeira x30, Carvão x4, Barra de Ferro x4, Quartzo Refinado x1 | 20 EU/tick | 20% mais rápida |
| Barril de Metal | Barra de Ferro x6, Barra de Cobre x4, Quartzo Refinado x1 | 10 EU/tick | 20% mais rápido |
| Barril de Irídio Rígido | Barra de Irídio x4, Barra de Ferro x2, Quartzo Refinado x1 | 30 EU/tick | 30% mais rápido |
| Barril de Envelhecimento de Metal | Madeira de Lei x8, Barra de Ferro x6, Barra de Irídio x2, Quartzo Refinado x1 | 40 EU/tick | 50% mais rápido para envelhecer |

O Barril de Irídio Rígido foi pensado para usar o comportamento do Barril de Madeira de Lei do Grapes of Ferngill. Sem Grapes of Ferngill, ele usa o comportamento do Barril vanilla.

## Progressão Sugerida

Início:

- Gerador a Vapor
- Cabo de Cobre
- Bateria de Energia Básica
- algumas Jarras de Conserva Industriais ou Barris de Metal

Meio do jogo:

- Biocombustível
- Gerador de Combustão
- Cabo de Ferro
- salas maiores de máquinas

Mais tarde:

- Cabo de Irídio
- Cabo de Irídio Energizado
- Gerador Radioisotópico
- Bateria de Energia de Irídio
- Conduítes de Energia
- Geradores Eólicos ao ar livre
- Barris de Envelhecimento de Metal e Barris de Irídio Rígido

## Solução de Problemas

Se uma máquina não recebe energia:

- Veja se ela está conectada por cabo ou encostada em outra máquina energizada.
- Veja se o gerador está abastecido ou produzindo energia.
- Veja se a máquina está processando algum item.
- Confira a Aba de Energia para ver rede, demanda, armazenamento e estado das máquinas.

Se um Gerador Eólico está offline:

- Veja se ele está ao ar livre.

Se combustível parece estar sendo desperdiçado:

- Adicione baterias para guardar EU sobrando.

Se uma ligação de conduíte está errada:

- Use a aba Conduítes na Aba de Energia.
- Shift + clique direito em um conduíte para remover a ligação ou cancelar pareamento.

## Mods Recomendados

PowerGrid funciona sozinho, mas fica melhor com:

- Grapes of Ferngill
- Automate ou Event Driven Automation
- Generic Mod Config Menu
