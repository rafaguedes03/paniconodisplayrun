using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PanicoNoDisplay
{
    /// <summary>
    /// HUD do modo infinito: score (distância percorrida), recorde, música em loop,
    /// botões de som/pause, tela de pause e tela de fim com cadastro do nome no TOP 5.
    /// </summary>
    public class PNDHud : MonoBehaviour
    {
        /// <summary>Quantos pixels de corrida valem 1 ponto de score.</summary>
        public const float PixelsPerPoint = 10f;

        /// <summary>A cada tantos px/s a mais de velocidade aparece o aviso "MAIS RÁPIDO!".</summary>
        public const float SpeedStep = 40f;
        /// <summary>Tom/andamento da música na velocidade máxima (1 = normal).</summary>
        public const float MaxMusicPitch = 1.12f;
        /// <summary>Segundos parado na tela de fim até voltar sozinho para o menu.</summary>
        public const float AutoMenuSeconds = 20f;
        /// <summary>Segundos sem digitar nada na tela do nome até pular sozinho.</summary>
        public const float NameIdleSeconds = 45f;

        public int score;
        public bool gameEnded;
        public bool isPaused;

        public PNDGame game;
        public PNDPlayer player;

        TextMeshProUGUI scoreLabel, recordLabel, titleLabel, reasonLabel, finalScoreLabel, rankMsgLabel, hintLabel;
        GameObject endScreen, pauseScreen, nameEntry;
        RectTransform boardHolder;
        TMP_InputField nameInput;
        PNDArtButton continueButton, restartButton, quitButton;
        PNDArtButton saveButton, skipButton;
        PNDArtButton muteButton, pauseButton;
        AudioSource musicPlayer, recordSound, speedSound;

        static readonly Color Verde = new Color(0.37f, 0.99f, 0.57f); // verde neon do menu
        Image recordFrame;

        TextMeshProUGUI speedBanner;
        float bannerTimer;
        int speedLevel;
        const float BannerSeconds = 1.5f;

        float idleSince;     // último momento em que alguém mexeu no teclado/mouse na tela de fim
        int shownCountdown = -1;
        const string FinalHint = "ESPAÇO jogar de novo   •   ESC voltar ao menu";

        int record;          // melhor score salvo quando a corrida começou
        bool beatRecord;
        bool enteringName;   // tela de fim esperando o nome do jogador
        float finalShownAt;  // evita reiniciar sem querer com a tecla que já estava apertada

        // ==========================================
        // Montagem
        // ==========================================
        public static PNDHud Create(Transform canvasRoot, PNDGame game)
        {
            var rt = PNDUI.Full(PNDUI.Rect("HUD", canvasRoot));
            var hud = rt.gameObject.AddComponent<PNDHud>();
            hud.game = game;
            hud.Build(rt);
            return hud;
        }

        void Build(RectTransform root)
        {
            record = PNDScores.Best;

            // ---------- Barra superior ----------
            var hbox = PNDUI.G(PNDUI.Rect("HBoxContainer", root), 0, 0, 1, 0, 20, 20, -20, 50);
            PNDUI.HBox(hbox, 8f, TextAnchor.MiddleLeft);

            // Caixa do SCORE (moldura de botão, com o número rodando dentro)
            var scoreBox = PNDUI.FrameBox("ScoreBox", hbox, out _);
            PNDUI.Layout(scoreBox, 150, 30, 150, 30);
            PNDUI.HBox(scoreBox, 6f, TextAnchor.MiddleLeft).padding = new RectOffset(12, 12, 0, 0);
            PNDUI.Label("ScoreTitle", scoreBox, "SCORE", 11, new Color(0.55f, 0.75f, 0.95f), TextAlignmentOptions.MidlineLeft);
            scoreLabel = PNDUI.Label("ScoreLabel", scoreBox, "0", 20, Color.white, TextAlignmentOptions.MidlineRight, 4, Color.black);
            PNDUI.Layout(scoreLabel, flexW: 1);

            var spacer = PNDUI.Rect("Spacer", hbox);
            PNDUI.Layout(spacer, flexW: 1);

            // Caixa do RECORDE (troféu + número); fica verde quando o recorde é batido
            var recordBox = PNDUI.FrameBox("RecordBox", hbox, out recordFrame);
            PNDUI.Layout(recordBox, 120, 30, 120, 30);
            PNDUI.HBox(recordBox, 6f, TextAnchor.MiddleLeft).padding = new RectOffset(10, 12, 0, 0);
            var trophyRt = PNDUI.Rect("Trophy", recordBox);
            var trophy = trophyRt.gameObject.AddComponent<Image>();
            trophy.sprite = PND.Spr("art/menu/icon_trophy");
            trophy.preserveAspect = true;
            trophy.raycastTarget = false;
            PNDUI.Layout(trophy, 18, 18, 18, 18);
            recordLabel = PNDUI.Label("RecordLabel", recordBox, record.ToString(), 18, new Color(1f, 0.85f, 0.3f), TextAlignmentOptions.MidlineRight, 4, Color.black);
            PNDUI.Layout(recordLabel, flexW: 1);

            SetupTopButtons(hbox);

            // ---------- Aviso "MAIS RÁPIDO!" ----------
            speedBanner = PNDUI.Label("SpeedBanner", root, "MAIS RÁPIDO!", 26, Verde, TextAlignmentOptions.Center, 5, Color.black);
            PNDUI.G(speedBanner.rectTransform, 0.5f, 0, 0.5f, 0, -200, 66, 200, 106);
            speedBanner.gameObject.SetActive(false);

            // ---------- Tela de pause ----------
            var pause = PNDUI.Full(PNDUI.Rect("PauseScreen", root));
            pauseScreen = pause.gameObject;
            PNDUI.ColorRect("ColorRect", pause, new Color(0f, 0.01f, 0.04f, 0.78f));
            var pp = PNDUI.FramePanel("Panel", pause, true);
            PNDUI.G(pp.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, -125, -112, 125, 112);
            var pv = PNDUI.G(PNDUI.Rect("VBoxContainer", pp.transform), 0, 0, 1, 1, 34, 12, -34, -16);
            PNDUI.VBox(pv, 11f);
            PNDUI.Label("PauseTitle", pv, "PAUSADO", 28, Verde, TextAlignmentOptions.Center, 5, Color.black);
            continueButton = PNDArtButton.CreateFramed("ContinueButton", pv, "CONTINUAR", 17);
            restartButton = PNDArtButton.CreateFramed("RestartButton", pv, "REINICIAR", 17);
            quitButton = PNDArtButton.CreateFramed("QuitButton", pv, "SAIR", 17);
            // ↑ ↓ circulam pelos três botões
            var pauseList = new Selectable[] { continueButton, restartButton, quitButton };
            for (int i = 0; i < pauseList.Length; i++)
            {
                pauseList[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = pauseList[(i + 2) % 3],
                    selectOnDown = pauseList[(i + 1) % 3],
                };
            }

            // ---------- Tela de fim ----------
            var end = PNDUI.Full(PNDUI.Rect("EndScreen", root));
            endScreen = end.gameObject;
            PNDUI.ColorRect("ColorRect", end, new Color(0f, 0.01f, 0.04f, 0.82f));
            var ev = PNDUI.G(PNDUI.Rect("VBoxContainer", end), 0.5f, 0.5f, 0.5f, 0.5f, -230, -50, 230, 50);
            // A moldura fica no próprio quadro, que cresce junto com o conteúdo
            var evFrame = ev.gameObject.AddComponent<Image>();
            PNDUI.Frame(evFrame, "art/menu/frame_panel_green", 26f);
            evFrame.raycastTarget = false;
            PNDUI.VBox(ev, 7f, TextAnchor.MiddleCenter).padding = new RectOffset(22, 22, 14, 16);
            // A altura acompanha o conteúdo e cresce para os dois lados
            var fitter = ev.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            titleLabel = PNDUI.Label("TitleLabel", ev, "FIM DE JOGO!", 28, new Color(1f, 0.2f, 0.2f), TextAlignmentOptions.Center, 4, Color.black);
            reasonLabel = PNDUI.Label("ReasonLabel", ev, "", 14, Color.white, TextAlignmentOptions.Center);
            finalScoreLabel = PNDUI.Label("FinalScoreLabel", ev, "SCORE: 0", 24, Verde, TextAlignmentOptions.Center, 4, Color.black);
            rankMsgLabel = PNDUI.Label("RankMsgLabel", ev, "", 14, Verde, TextAlignmentOptions.Center);

            // Linha para digitar o nome (só aparece quando o score entra no TOP 5)
            var entry = PNDUI.Rect("NameEntry", ev);
            nameEntry = entry.gameObject;
            PNDUI.HBox(entry, 8f, TextAnchor.MiddleCenter);
            PNDUI.Label("NameLabel", entry, "NOME:", 16, Color.white, TextAlignmentOptions.MidlineLeft);
            nameInput = PNDUI.TextInput("NameInput", entry, "digite aqui", 16, PNDScores.MaxNameLength);
            PNDUI.Layout(nameInput, 150, 28, 150, 28);
            nameInput.onValidateInput = (text, index, ch) => char.ToUpperInvariant(ch); // tudo em maiúsculas
            saveButton = PNDArtButton.CreateFramed("SaveButton", entry, "SALVAR", 13);
            PNDUI.Layout(saveButton, 80, 28, 80, 28);
            skipButton = PNDArtButton.CreateFramed("SkipButton", entry, "PULAR", 13);
            PNDUI.Layout(skipButton, 70, 28, 70, 28);

            // Onde o quadro do TOP 5 aparece depois de salvar/pular
            boardHolder = PNDUI.Rect("BoardHolder", ev);
            PNDUI.HBox(boardHolder, 0f, TextAnchor.MiddleCenter);

            hintLabel = PNDUI.Label("HintLabel", ev, "", 13, new Color(0.85f, 0.85f, 0.9f), TextAlignmentOptions.Center);

            // ---------- Sons ----------
            recordSound = PND.Audio(gameObject, "audio/sfx/powerUp2");
            speedSound = PND.Audio(gameObject, "audio/sfx/twoTone1", -10f);
            SetupMusic();

            continueButton.onClick.AddListener(OnContinuePressed);
            restartButton.onClick.AddListener(OnRestartPressed);
            quitButton.onClick.AddListener(OnQuitPressed);
            saveButton.onClick.AddListener(SaveName);
            skipButton.onClick.AddListener(SkipName);

            endScreen.SetActive(false);
            pauseScreen.SetActive(false);
        }

        // ==========================================
        // Entrada (P = pause, M = som) — funciona mesmo pausado
        // ==========================================
        void Update()
        {
            var kb = Keyboard.current;

            if (!gameEnded)
            {
                UpdateScore();
                UpdateSpeedFeedback();
                if (kb != null)
                {
                    if (kb.pKey.wasPressedThisFrame)
                        TogglePause();
                    else if (kb.mKey.wasPressedThisFrame)
                        ToggleMute();
                }
                return;
            }

            if (kb == null) return;

            // Alguém mexeu? Zera a contagem de inatividade.
            var mouse = Mouse.current;
            if (kb.anyKey.wasPressedThisFrame || (mouse != null && mouse.leftButton.wasPressedThisFrame))
                idleSince = Time.unscaledTime;
            float idle = Time.unscaledTime - idleSince;

            if (enteringName)
            {
                // Digitando o nome: ENTER salva, ESC pula (ESPAÇO é só mais uma letra)
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                    SaveName();
                else if (kb.escapeKey.wasPressedThisFrame || idle >= NameIdleSeconds)
                    SkipName(); // ninguém digitou: segue para o ranking
                return;
            }

            if (Time.unscaledTime - finalShownAt < 0.35f) return;

            if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                RestartGame();
                return;
            }
            if (kb.escapeKey.wasPressedThisFrame || idle >= AutoMenuSeconds)
            {
                // Volta para o menu (onde fica o TOP 5); o SAIR do menu volta ao laboratório.
                // Também acontece sozinho se ninguém mexer, deixando a máquina pronta para o próximo.
                SetTreePaused(false);
                game.ShowMenu();
                return;
            }

            // Contagem regressiva nos últimos segundos
            int left = Mathf.CeilToInt(AutoMenuSeconds - idle);
            if (left > 10) left = -1;
            if (left != shownCountdown)
            {
                shownCountdown = left;
                hintLabel.text = left < 0 ? FinalHint : FinalHint + "\nVoltando ao menu em " + left + " s...";
            }
        }

        // ==========================================
        // Sensação de velocidade: música acelera e aviso a cada marco
        // ==========================================
        void UpdateSpeedFeedback()
        {
            if (player == null) return;
            float speed = player.runSpeed;

            if (musicPlayer != null)
                musicPlayer.pitch = Mathf.Lerp(1f, MaxMusicPitch, Mathf.InverseLerp(PNDLevel.StartSpeed, PNDLevel.MaxSpeed, speed));

            int level = Mathf.FloorToInt((speed - PNDLevel.StartSpeed) / SpeedStep);
            if (level > speedLevel)
            {
                speedLevel = level;
                speedBanner.text = speed >= PNDLevel.MaxSpeed ? "VELOCIDADE INSANA!" : "MAIS RÁPIDO!";
                speedBanner.gameObject.SetActive(true);
                bannerTimer = BannerSeconds;
                if (speedSound != null) speedSound.Play();
            }

            if (bannerTimer > 0f)
            {
                bannerTimer -= Time.deltaTime;
                if (bannerTimer <= 0f)
                {
                    speedBanner.gameObject.SetActive(false);
                }
                else
                {
                    // Entra "estourando" e some no final
                    float age = BannerSeconds - bannerTimer;
                    float pop = 1f + 0.35f * Mathf.Clamp01(1f - age / 0.2f);
                    speedBanner.rectTransform.localScale = new Vector3(pop, pop, 1f);
                    speedBanner.alpha = Mathf.Clamp01(bannerTimer / 0.4f);
                }
            }
        }

        // ==========================================
        // Score = distância percorrida
        // ==========================================
        void UpdateScore()
        {
            if (player == null) return;
            int s = Mathf.FloorToInt(player.Distance / PixelsPerPoint);
            if (s <= score) return;
            score = s;
            scoreLabel.text = score.ToString();

            if (!beatRecord && record > 0 && score > record)
            {
                // Recorde batido: a caixa fica verde e passa a acompanhar o score
                beatRecord = true;
                recordLabel.color = Verde;
                if (recordFrame != null) PNDUI.Frame(recordFrame, "art/menu/frame_btn_hover", 50f);
                if (recordSound != null) recordSound.Play();
            }
            if (beatRecord)
                recordLabel.text = score.ToString();
        }

        public void TogglePause()
        {
            if (gameEnded)
                return;
            // Não pausa durante a queda que encerra a corrida
            if (!isPaused && player != null && player.isHurt)
                return;
            isPaused = !isPaused;
            SetTreePaused(isPaused);
            if (isPaused)
            {
                pauseScreen.SetActive(true);
                // Botões começam azuis; a primeira seta seleciona o "Continuar"
                PNDUI.Select(null);
                game.DefaultSelectable = continueButton;
            }
            else
            {
                pauseScreen.SetActive(false);
                game.DefaultSelectable = null;
                PNDUI.Select(null);
            }
        }

        void OnContinuePressed()
        {
            if (isPaused)
                TogglePause();
        }

        void OnRestartPressed()
        {
            isPaused = false;
            RestartGame();
        }

        void OnQuitPressed()
        {
            isPaused = false;
            SetTreePaused(false);
            // Volta para a tela de início do minijogo
            game.ShowMenu();
        }

        // ==========================================
        // Fim de jogo + TOP 5
        // ==========================================
        public void ShowGameOver(string reason = "Você bateu!")
        {
            if (gameEnded) return;
            UpdateScore();
            gameEnded = true;
            isPaused = false;
            pauseScreen.SetActive(false);
            game.DefaultSelectable = null;
            speedBanner.gameObject.SetActive(false);
            idleSince = Time.unscaledTime;

            reasonLabel.text = reason;
            finalScoreLabel.text = "SCORE: " + score;
            endScreen.SetActive(true);
            SetTreePaused(true);

            if (PNDScores.Qualifies(score))
            {
                enteringName = true;
                rankMsgLabel.color = new Color(0.3f, 1f, 0.3f);
                rankMsgLabel.text = score > PNDScores.Best
                    ? "NOVO RECORDE! Digite seu nome para salvar:"
                    : "Você entrou no TOP " + PNDScores.TopCount + "! Digite seu nome para salvar:";
                boardHolder.gameObject.SetActive(false);
                nameEntry.SetActive(true);
                nameInput.text = ""; // o nome nunca fica guardado: cada jogador digita o seu
                hintLabel.text = "ENTER salvar   •   ESC pular";
                PNDUI.Select(nameInput);
                nameInput.ActivateInputField();
            }
            else
            {
                ShowFinal(-1, false);
            }
        }

        void SaveName()
        {
            if (!enteringName) return;
            string n = nameInput.text != null ? nameInput.text.Trim().ToUpperInvariant() : "";
            int index = PNDScores.Add(n, score);
            ShowFinal(index, true);
        }

        void SkipName()
        {
            if (!enteringName) return;
            ShowFinal(-1, false);
        }

        /// <summary>Segunda parte da tela de fim: mostra o TOP 5 e as opções de jogar de novo / menu.</summary>
        void ShowFinal(int highlight, bool saved)
        {
            enteringName = false;
            if (nameInput != null) nameInput.DeactivateInputField();
            nameEntry.SetActive(false);

            var top = PNDScores.Top;
            if (saved && highlight >= 0)
            {
                rankMsgLabel.color = new Color(0.3f, 1f, 0.3f);
                rankMsgLabel.text = highlight == 0 ? "Score salvo: você é o novo 1º lugar!" : "Score salvo: " + (highlight + 1) + "º lugar!";
            }
            else if (score > 0 && top.Count >= PNDScores.TopCount && score <= top[top.Count - 1].score)
            {
                rankMsgLabel.color = new Color(0.85f, 0.85f, 0.9f);
                rankMsgLabel.text = "Faltaram " + (top[top.Count - 1].score - score + 1) + " pontos para entrar no TOP " + PNDScores.TopCount;
            }
            else
            {
                rankMsgLabel.text = "";
            }
            rankMsgLabel.gameObject.SetActive(rankMsgLabel.text.Length > 0);

            // Recria o quadro com o ranking atualizado
            for (int i = boardHolder.childCount - 1; i >= 0; i--)
                Destroy(boardHolder.GetChild(i).gameObject);
            // Ativa ANTES de montar: o TextMeshPro não aceita contorno em texto criado dentro de objeto desativado
            boardHolder.gameObject.SetActive(true);
            var board = PNDScores.BuildBoard(boardHolder, highlight);
            PNDUI.Layout(board, 260, 168, 260, 168);

            hintLabel.text = FinalHint;
            shownCountdown = -1;
            PNDUI.Select(null);
            finalShownAt = Time.unscaledTime;
            idleSince = Time.unscaledTime;
        }

        void RestartGame()
        {
            SetTreePaused(false);
            game.RestartLevel();
        }

        /// <summary>get_tree().paused: para física, animações, timers da fase e a música.</summary>
        void SetTreePaused(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
            if (musicPlayer != null)
            {
                if (paused) musicPlayer.Pause();
                else musicPlayer.UnPause();
            }
            if (game != null) game.SetLevelAudioPaused(paused);
        }

        // ==========================================
        // MÚSICA DA CORRIDA (em loop enquanto a corrida durar)
        // ==========================================
        void SetupMusic()
        {
            musicPlayer = PND.Audio(gameObject, "audio/music/musica-corrida", -6f, loop: true);
            musicPlayer.loop = true;
            musicPlayer.Play(); // O personagem começa a correr assim que a fase carrega
        }

        // ==========================================
        // BOTÕES DE SOM E PAUSE (canto superior direito)
        // ==========================================
        void SetupTopButtons(RectTransform hbox)
        {
            // Sem foco: assim o ESPAÇO (pulo) não "aperta" o botão sem querer
            muteButton = PNDArtButton.CreateFramed("MuteButton", hbox, "", 16);
            muteButton.SetIcon("art/menu/icon_sound_on", 18);
            muteButton.NoKeyboard();
            PNDUI.Layout(muteButton, 34, 28, 34, 28);
            muteButton.onClick.AddListener(ToggleMute);

            pauseButton = PNDArtButton.CreateFramed("PauseButton", hbox, "", 16);
            pauseButton.SetIcon("art/menu/icon_pause", 16);
            pauseButton.NoKeyboard();
            PNDUI.Layout(pauseButton, 34, 28, 34, 28);
            pauseButton.onClick.AddListener(TogglePause);

            UpdateMuteButton();
        }

        public void ToggleMute()
        {
            // Muta tudo (música e efeitos) e continua mutado mesmo ao reiniciar a fase
            PND.Muted = !PND.Muted;
            PND.ApplyMute();
            UpdateMuteButton();
        }

        void UpdateMuteButton()
        {
            if (muteButton == null) return;
            muteButton.SetIcon(PND.Muted ? "art/menu/icon_sound_off" : "art/menu/icon_sound_on", 18);
        }
    }
}
