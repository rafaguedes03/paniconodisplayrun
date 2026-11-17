using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PanicoNoDisplay
{
    /// <summary>
    /// Abre e fecha o minijogo "Pânico no Display" dentro do jogo principal.
    ///
    /// Ao abrir: guarda o estado do jogo principal, desativa os objetos das cenas carregadas
    /// (nada é descarregado, então posição do jogador, dinheiro etc. ficam intactos)
    /// e cria uma cena nova só para o minijogo.
    /// Ao fechar: descarrega a cena do minijogo, reativa tudo, restaura cursor/tempo/som
    /// e entrega a recompensa (pontos x 0.1) na carteira do jogador.
    /// </summary>
    public static class PanicoNoDisplayLauncher
    {
        /// <summary>Quanto cada ponto vale em dinheiro no jogo principal (somente em caso de vitória).</summary>
        public const float MoneyPerPoint = 0.1f;

        public static bool IsRunning { get; private set; }

        /// <summary>Dinheiro ganho nesta sessão do minijogo, entregue ao voltar.</summary>
        public static int PendingMoney { get; private set; }

        /// <summary>Disparado ao voltar para o jogo principal, com o dinheiro entregue.</summary>
        public static event System.Action<int> OnClosed;

        static Scene originScene;
        static Scene miniScene;
        static readonly List<GameObject> disabledRoots = new List<GameObject>();

        static CursorLockMode prevLock;
        static bool prevCursorVisible;
        static float prevTimeScale = 1f;
        static float prevFixedDelta = 0.02f;
        static float prevListenerVolume = 1f;
        static bool prevListenerPause;

        static void Log(string msg) => Debug.Log("[Pânico no Display] " + msg);

        // O Unity 6 pode entrar no Play sem recarregar os scripts ("Enter Play Mode Options"):
        // nesse caso as variáveis estáticas sobrevivem de um Play para o outro. Zera tudo
        // no início de cada Play, senão o minijogo "acha" que ainda está aberto.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IsRunning = false;
            PendingMoney = 0;
            disabledRoots.Clear();
            originScene = default;
            miniScene = default;
        }

        public static void Launch()
        {
            // Só considera "aberto" se a cena do minijogo realmente ainda existe
            if (IsRunning && !(miniScene.IsValid() && miniScene.isLoaded))
                IsRunning = false;

            if (IsRunning)
            {
                Log("Launch ignorado: o minijogo já está aberto.");
                return;
            }
            Log("Abrindo o minijogo...");
            IsRunning = true;
            PendingMoney = 0;

            originScene = SceneManager.GetActiveScene();

            // Guarda o estado do jogo principal
            prevLock = Cursor.lockState;
            prevCursorVisible = Cursor.visible;
            prevTimeScale = Time.timeScale;
            prevFixedDelta = Time.fixedDeltaTime;
            prevListenerVolume = AudioListener.volume;
            prevListenerPause = AudioListener.pause;

            // "Congela" o jogo principal desativando os objetos raiz de todas as cenas carregadas
            disabledRoots.Clear();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var sc = SceneManager.GetSceneAt(i);
                if (!sc.isLoaded) continue;
                foreach (var go in sc.GetRootGameObjects())
                {
                    if (go.activeSelf)
                    {
                        disabledRoots.Add(go);
                        go.SetActive(false);
                    }
                }
            }

            // Física igual ao Godot (60 passos por segundo)
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 1f / 60f;
            AudioListener.pause = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            miniScene = SceneManager.CreateScene("PanicoNoDisplay_" + System.DateTime.Now.Ticks);
            SceneManager.SetActiveScene(miniScene);

            var root = new GameObject("PanicoNoDisplay");
            SceneManager.MoveGameObjectToScene(root, miniScene);
            root.AddComponent<PNDGame>();
            Log("Minijogo aberto. Jogo principal pausado (" + disabledRoots.Count + " objetos raiz desativados).");
        }

        /// <summary>Registra uma vitória e devolve quanto dinheiro ela vale.</summary>
        public static int RegisterVictory(int points)
        {
            int reward = Mathf.RoundToInt(points * MoneyPerPoint);
            PendingMoney += reward;
            Log("Vitória com " + points + " pontos = $" + reward + " (total a receber: $" + PendingMoney + ").");
            return reward;
        }

        public static void Exit()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Log("Saindo do minijogo e voltando ao jogo principal...");

            // Desliga o minijogo antes de religar o jogo principal (evita 2 câmeras/EventSystems/AudioListeners)
            if (miniScene.IsValid() && miniScene.isLoaded)
            {
                foreach (var go in miniScene.GetRootGameObjects())
                    go.SetActive(false);
            }

            Time.timeScale = prevTimeScale <= 0f ? 1f : prevTimeScale;
            Time.fixedDeltaTime = prevFixedDelta;
            AudioListener.volume = prevListenerVolume;
            AudioListener.pause = prevListenerPause;

            foreach (var go in disabledRoots)
            {
                if (go != null) go.SetActive(true);
            }
            disabledRoots.Clear();

            if (originScene.IsValid() && originScene.isLoaded)
                SceneManager.SetActiveScene(originScene);

            if (miniScene.IsValid() && miniScene.isLoaded)
                SceneManager.UnloadSceneAsync(miniScene);

            Cursor.lockState = prevLock;
            Cursor.visible = prevCursorVisible;

            // Entrega a recompensa
            int money = PendingMoney;
            PendingMoney = 0;
            if (money > 0)
            {
                // Procura o PlayerWallet pelo nome: assim o minijogo também compila e roda
                // sozinho, em um projeto que não tem a carteira do jogo principal.
                var walletType = System.Type.GetType("PlayerWallet");
                var wallet = walletType != null ? Object.FindAnyObjectByType(walletType) : null;
                var add = wallet != null ? walletType.GetMethod("AdicionarDinheiro") : null;
                if (add != null && add.GetParameters().Length == 1)
                {
                    add.Invoke(wallet, new object[] { System.Convert.ChangeType(money, add.GetParameters()[0].ParameterType) });
                    Log("Recompensa de $" + money + " entregue.");
                }
                else
                    Debug.LogWarning("[Pânico no Display] PlayerWallet não encontrado para entregar $" + money);
            }

            else
            {
                Log("Sem recompensa desta vez (só vitórias dão dinheiro).");
            }

            OnClosed?.Invoke(money);
        }
    }
}
