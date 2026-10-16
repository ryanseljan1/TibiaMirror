# TibiaMirror

[English](README.en.md)

Espelhe qualquer parte da tela do Tibia onde quiser — barras de vida, tempos de recarga, minimapa — e use temporizadores com som e barras de contagem regressiva.

**Gratuito e de código aberto:** sem anúncios, sem chave de licença, só se conecta ao GitHub para verificar atualizações (opcional), sem telemetria, espelhos ilimitados — todos os recursos liberados. Não lê a memória do jogo nem envia teclas ou cliques.

> [!TIP]
> [![Baixar v0.1.1](https://img.shields.io/badge/Baixar-v0.1.1-FF7F00?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/ryanseljan1/TibiaMirror/releases/latest/download/TibiaMirror-Setup.exe)
>
> **TibiaMirror 0.1.1 (correção):** os cliques sobre os espelhos voltam a chegar ao Tibia, e espelhos destravados também deixam o clique passar enquanto o Tibia está em foco. **[Ver as notas da versão →](https://github.com/ryanseljan1/TibiaMirror/releases/tag/v0.1.1)**

<!-- Capturas de tela em breve. Adicione docs/images/overview.png e descomente:
![Visão geral do TibiaMirror](docs/images/overview.png)
-->

## ⬇ Instalação (3 passos)

1. Acesse a **[página de versões](../../releases)** e baixe `TibiaMirror-Setup-<versão>.exe`.
2. Execute o instalador. Se o Windows mostrar “O Windows protegeu o computador”, clique em **Mais informações → Executar assim mesmo**. O TibiaMirror é gratuito e não tem assinatura de código; por isso, o Windows ainda não reconhece o editor.
3. Abra o TibiaMirror pelo menu Iniciar e depois inicie o Tibia. O indicador na parte superior ficará verde.

### Opção sem instalação

Baixe `TibiaMirror-Portable-<versão>.zip` na página de versões. Clique nele com o botão direito, escolha **Extrair Tudo** e execute `TibiaMirror.exe`.

## Primeiros passos: seu primeiro espelho

<!-- Capturas de tela em breve. Adicione docs/images/add-region.gif e descomente:
![Adicionando uma região](docs/images/add-region.gif)
-->

1. Inicie o Tibia e espere até o indicador do TibiaMirror ficar verde.
2. Na aba **Mirrors**, clique em **＋ Add Region**.
3. Arraste para selecionar a parte do Tibia que deseja copiar, como a barra de vida. Pressione Esc ou clique com o botão direito para cancelar.
4. Uma nova janela de espelho aparecerá. Arraste-a para onde quiser e puxe as bordas para redimensioná-la.
5. Clique no cadeado do cartão do espelho para permitir que os cliques atravessem a janela, sem atrapalhar o jogo.

## Recursos

### Espelhos

- Adicione quantas regiões quiser. Cada uma tem sua própria janela e um cartão na lista.
- Mova e redimensione os espelhos livremente. Use **Re-crop** para selecionar outra área.
- **Lock** permite clicar através do espelho. **Lock all** bloqueia todos de uma vez.
- O botão de olho mostra ou oculta um espelho, e o lápis altera seu nome. **Delete** remove o espelho. O controle de opacidade ajusta sua transparência.
- **Show/Hide all** mostra ou oculta todos os espelhos. Você também pode configurar uma tecla de atalho para isso nas configurações (⚙).
- Marque “Only show mirrors while Tibia is focused” nas configurações (⚙) para ocultar os espelhos ao mudar para outro programa. O Tibia ou uma janela de espelho contam como ativos; a janela principal, os diálogos e as configurações do TibiaMirror não contam.
- Marque **Dark mode** nas configurações (⚙) e clique em **Save** para alterar o tema do aplicativo. A escolha será restaurada na próxima abertura.

### Barra de contagem regressiva

- Cada espelho pode ter uma barra que diminui durante o período escolhido. Abra a seção **Countdown** no cartão do espelho e marque **Enable countdown**.
- Escolha uma tecla de atalho. Ela funciona apenas quando o Tibia ou um espelho está ativo, nunca enquanto você digita no TibiaMirror. Se a tecla já estiver em uso, o aplicativo pedirá confirmação.
- Escolha o lado (esquerda, direita, em cima ou embaixo), o formato (**Full edge** ou **Narrow strip**), a cor e a espessura.
- A barra acompanha o espelho quando você o move. Pressionar a tecla novamente reinicia a contagem.
- Ao terminar, a borda pode piscar e um som pode tocar.

### Temporizadores com som

- Na aba **Audio Timers**, clique em **＋ Add Timer**. Escolha uma tecla de atalho e uma duração.
- Pressione a tecla no Tibia para iniciar o temporizador. Quando ele terminar, um som tocará.
- Use um som incluído no aplicativo ou um arquivo de áudio seu. **Test** reproduz o som.
- Escolha o que acontece ao pressionar a tecla durante a contagem: reiniciar ou ignorar o comando.
- **Listening: ON/OFF** ativa ou desativa todas as teclas de atalho dos temporizadores. Também há um controle de volume geral.
- “Start listening when the app opens” define o estado de **Listening** a cada abertura: marcado inicia em ON; desmarcado inicia em OFF, independentemente do estado ao fechar o aplicativo.

### Perfis

- Um perfil guarda uma configuração completa: espelhos, temporizadores e contagens regressivas. Na aba **Profiles**, use **New**, **Rename**, **Duplicate** e **Delete**; use **Switch** para trocar de perfil.
- Configure a tecla “Next profile” nas configurações (⚙) para trocar rapidamente. O aplicativo avisa se ela conflitar com outra tecla de atalho e mostra brevemente o nome do novo perfil.
- **Export...** salva um perfil em um arquivo; **Import...** carrega um arquivo. Assim você pode compartilhar configurações ou transferi-las para outro PC.

## É seguro? É permitido?

O TibiaMirror mostra apenas uma imagem ao vivo da janela do Tibia, usando o mesmo recurso do Windows empregado nas prévias da barra de tarefas, e monitora as teclas de atalho que você escolheu. Ele não lê nem altera o jogo, não pressiona teclas por você e não bloqueia suas teclas.

Siga as regras do Tibia e use o TibiaMirror por sua conta e risco.

## Perguntas frequentes e solução de problemas

**Meus espelhos estão pretos ou não aparecem.**

O Tibia precisa estar aberto e não pode estar minimizado. Execute o TibiaMirror e o Tibia com o mesmo nível de permissão: ambos normalmente ou ambos como administrador.

**Minhas teclas de atalho não funcionam.**

O Tibia ou um espelho precisa estar ativo; a janela principal do TibiaMirror não conta. **Listening** precisa estar em ON na aba **Audio Timers**. Outro programa pode estar usando a tecla; tente outra. Se aparecer “Hotkeys are unavailable.”, clique em **Retry**.

**Uso mais de um monitor.**

Os espelhos podem ficar em qualquer monitor. Se um monitor não estiver disponível na próxima abertura, os espelhos dele serão movidos para o monitor principal.

**Onde ficam minhas configurações?**

Em `%APPDATA%\TibiaMirror`. Abra as configurações (⚙) e clique em **Open data folder** para acessar a pasta.

**Como transfiro minhas configurações para outro PC?**

Na aba **Profiles**, clique em **Export...** no PC antigo e em **Import...** no novo.

**Como desinstalo?**

Acesse Configurações do Windows → Aplicativos → TibiaMirror → Desinstalar. Suas configurações são mantidas; exclua `%APPDATA%\TibiaMirror` para removê-las também. Para a versão portátil, exclua a pasta extraída.

## Para desenvolvedores

Você precisa do [SDK do .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) no Windows.

```
dotnet build TibiaMirror.sln -c Debug
dotnet test tests/TibiaMirror.Tests -c Debug
dotnet run --project src/TibiaMirror
```

Estrutura do projeto:

- `src/TibiaMirror` — aplicativo WPF (espelhos, temporizadores, contagem regressiva, perfis, configurações, atalhos, detecção e integração com o Windows).
- `tests/TibiaMirror.Tests` — testes automatizados.
- `installer` — script do Inno Setup.
- `tools` — scripts auxiliares (`make-icon.ps1` cria o ícone do aplicativo).
- `docs` — checklist de testes manuais e notas de design.

Para criar uma versão, crie uma tag e envie-a ao GitHub. O GitHub Actions compila, testa e publica o instalador e o arquivo ZIP portátil.

```
git tag v1.2.3 && git push --tags
```

## Como contribuir

Issues e pull requests são bem-vindos.

- Execute `dotnet test tests/TibiaMirror.Tests` antes de abrir um pull request e adicione testes para novas regras de funcionamento.
- Regra de desenvolvimento independente: não copie código nem recursos de outras ferramentas. Escreva seu próprio código e mantenha sons e imagens originais.
- Regra de segurança do jogo: o TibiaMirror nunca deve ler nem alterar a memória do jogo, injetar código, enviar comandos de entrada ou bloquear teclas. Mudanças desse tipo não serão aceitas.

## Créditos e licença

Publicado sob a licença MIT (consulte `LICENSE`). Os sons são originais e sintetizados. O TibiaMirror não é afiliado à CipSoft. “Tibia” é uma marca registrada da CipSoft GmbH.
