using System.Collections.Generic;
using UnityEngine;

namespace PanicoNoDisplay
{
    /// <summary>
    /// Monta a fase do MODO INFINITO: fundo, chão que acompanha o jogador, player
    /// e o gerador aleatório de obstáculos (PNDSpawner). As "cenas" de cada obstáculo
    /// continuam com os mesmos tamanhos do jogo original.
    /// </summary>
    public static class PNDLevel
    {
        // Ordem na árvore do Godot = ordem de desenho (z_index 0). O Player tem z_index 1.
        const int OrderBackground = -100;
        const int OrderGround = 0;
        const int OrderPlayer = 5000;

        // ---------- Dificuldade do modo infinito (ajuste à vontade) ----------
        /// <summary>Velocidade no começo da corrida (pixels por segundo).</summary>
        public const float StartSpeed = 200f;
        /// <summary>Fim da aceleração forte. Depois disso continua subindo, só que bem devagar.</summary>
        public const float MaxSpeed = 560f;
        /// <summary>Quanto a velocidade aumenta por pixel percorrido (0.015 = +15 a cada 1000 px).</summary>
        public const float SpeedPerPixel = 0.015f;
        /// <summary>Aceleração lenta depois de MaxSpeed (0.002 = +2 a cada 1000 px): ninguém joga para sempre.</summary>
        public const float LateSpeedPerPixel = 0.002f;
        /// <summary>Limite absoluto (acima disso sobra menos de 0,7 s para reagir).</summary>
        public const float HardMaxSpeed = 680f;

        /// <summary>A pressão extra (espaços menores, mais combos) começa aqui... (10000 px = 1000 pontos)</summary>
        public const float RampStart = 10000f;
        /// <summary>...e chega ao máximo aqui (3000 pontos).</summary>
        public const float RampEnd = 30000f;

        /// <summary>0 no começo da corrida, 1 na dificuldade máxima.</summary>
        public static float Pressure(float distance)
        {
            return Mathf.InverseLerp(RampStart, RampEnd, distance);
        }

        public const float PlayerStartX = 100f;
        public const float GroundWidth = 8192f; // múltiplo de 128 (largura do piso)

        /// <summary>Velocidade da corrida depois de percorrer 'distance' pixels.</summary>
        public static float SpeedAt(float distance)
        {
            distance = Mathf.Max(0f, distance);
            float fastEnd = (MaxSpeed - StartSpeed) / SpeedPerPixel; // onde a aceleração forte termina
            if (distance <= fastEnd)
                return StartSpeed + distance * SpeedPerPixel;
            return Mathf.Min(HardMaxSpeed, MaxSpeed + (distance - fastEnd) * LateSpeedPerPixel);
        }

        public static PNDPlayer Build(Transform root, PNDHud hud, Camera cam)
        {
            // ---------- Fundo com parallax ----------
            var bgSprite = PND.Spr("art/environment/display_lab_background_clean");
            var parallax = PND.Node("ParallaxBackground", root, Vector3.zero);
            var copies = new SpriteRenderer[2];
            for (int i = 0; i < copies.Length; i++)
            {
                copies[i] = PND.MakeSprite("BackgroundSprite" + i, parallax.transform, bgSprite, Vector3.zero, OrderBackground);
                copies[i].transform.localScale = new Vector3(0.5f, 0.58f, 1f);
            }

            // ---------- Chão (o PNDSpawner o reposiciona para nunca acabar) ----------
            var ground = PND.Node("Ground", root, PND.G(0, 300));
            var gcol = ground.AddComponent<BoxCollider2D>();
            gcol.size = new Vector2(GroundWidth, 128);
            var floor = PND.MakeSprite("Sprite2D", ground.transform, PND.Spr("art/environment/lab_floor_tile_128"), Vector3.zero, OrderGround);
            floor.drawMode = SpriteDrawMode.Tiled;
            floor.tileMode = SpriteTileMode.Continuous;
            floor.size = new Vector2(GroundWidth, 128);

            // ---------- Player ----------
            var player = PNDPlayer.Create(root, PND.G(PlayerStartX, 200), hud, OrderPlayer);
            if (hud != null) hud.player = player;

            // Câmera (Camera2D filha do Player no Godot)
            var follow = root.gameObject.AddComponent<PNDCameraFollow>();
            follow.cam = cam;
            follow.target = player.transform;
            follow.backgrounds = copies;

            // ---------- Obstáculos aleatórios, sem fim ----------
            var spawner = root.gameObject.AddComponent<PNDSpawner>();
            spawner.root = root;
            spawner.player = player;
            spawner.ground = ground.transform;

            return player;
        }

        /// <summary>Cria um obstáculo do tipo pedido dentro de 'parent'.</summary>
        internal static void Spawn(string type, Transform parent, Vector3 pos, PNDPlayer player, int order)
        {
            switch (type)
            {
                case "papers": SimpleObstacle("A4Papers", parent, pos, "art/obstacles/a4_papers", new Vector2(56, 14), 5, "slip", "Você escorregou nos papéis!", order); break;
                case "pencils": SimpleObstacle("Pencils", parent, pos, "art/obstacles/pencils", new Vector2(52, 14), 4, "slip", "Você escorregou nos lápis!", order); break;
                case "wires": SimpleObstacle("TangledWires", parent, pos, "art/obstacles/tangled_wires", new Vector2(54, 24), 6, "trip", "Você tropeçou nos fios!", order); break;
                case "cooler": WaterCooler("WaterCooler", parent, pos, order); break;
                case "nbobs": ObstacleNotebook("ObstacleNotebook", parent, pos, order); break;
                case "cleaner": Cleaner("Cleaner", parent, pos, player, order); break;
                case "ac": AirConditioner("AirConditioner", parent, pos, order); break;
                case "hwire": HangingWire("HangingWire", parent, pos, order); break;
            }
        }

        static BoxCollider2D Box(GameObject go, Vector2 size, float gOffsetX, float gOffsetY, bool trigger = true)
        {
            var c = go.AddComponent<BoxCollider2D>();
            c.size = size;
            c.offset = new Vector2(gOffsetX, -gOffsetY);
            c.isTrigger = trigger;
            return c;
        }

        static void SimpleObstacle(string name, Transform root, Vector3 pos, string sprite, Vector2 size, float offY, string damage, string reason, int order)
        {
            var go = PND.Node(name, root, pos);
            PND.MakeSprite("Sprite2D", go.transform, PND.Spr(sprite), Vector3.zero, order);
            Box(go, size, 0, offY);
            var o = go.AddComponent<PNDObstacle>();
            o.damageType = damage;
            o.reason = reason;
        }

        static void WaterCooler(string name, Transform root, Vector3 pos, int order)
        {
            var go = PND.Node(name, root, pos);
            var anim = PNDSpriteAnimator.Create("AnimatedSprite2D", go.transform, Vector3.zero, order,
                new PNDAnimation("spill", 5f, true, ("art/obstacles/water_cooler_1", 1f), ("art/obstacles/water_cooler_2", 1f)));
            anim.Play("spill");
            Box(go, new Vector2(64, 14), 10, 36); // a poça d'água
            var o = go.AddComponent<PNDObstacle>();
            o.damageType = "slip";
            o.reason = "Você escorregou na poça d'água!";
        }

        static void ObstacleNotebook(string name, Transform root, Vector3 pos, int order)
        {
            var go = PND.Node(name, root, pos);
            var anim = PNDSpriteAnimator.Create("AnimatedSprite2D", go.transform, Vector3.zero, order,
                new PNDAnimation("crack", 6f, false, ("art/obstacles/notebook_cracking", 1f), ("art/obstacles/notebook_cracked", 1f)),
                new PNDAnimation("intact", 5f, false, ("art/obstacles/notebook_intact", 1f)));
            anim.Play("intact");
            Box(go, new Vector2(50, 26), 0, 7);
            go.AddComponent<PNDObstacleNotebook>().anim = anim;
        }

        static void Cleaner(string name, Transform root, Vector3 pos, PNDPlayer player, int order)
        {
            var go = PND.Node(name, root, pos);
            var anim = PNDSpriteAnimator.Create("AnimatedSprite2D", go.transform, Vector3.zero, order,
                new PNDAnimation("attack", 7f, true, ("art/obstacles/cleaner_attack_1", 1f), ("art/obstacles/cleaner_attack_2", 1f)),
                new PNDAnimation("sweep", 4f, true, ("art/obstacles/cleaner_sweep_1", 1f), ("art/obstacles/cleaner_sweep_2", 1f)));
            anim.Play("sweep");
            Box(go, new Vector2(46, 48), 0, 10);
            var c = go.AddComponent<PNDCleaner>();
            c.anim = anim;
            c.player = player;
        }

        static void AirConditioner(string name, Transform root, Vector3 pos, int order)
        {
            var go = PND.Node(name, root, pos);
            PND.MakeSprite("ACUnit", go.transform, PND.Spr("art/obstacles/air_conditioner"), Vector3.zero, order);
            Box(go, new Vector2(128, 240), 0, 120);
            go.AddComponent<PNDAirConditioner>();

            // WindParticles (CPUParticles2D)
            var wind = PND.Node("WindParticles", go.transform, PND.G(0, 20)).AddComponent<PNDParticles>();
            wind.amount = 32;
            wind.lifetime = 0.75f;
            wind.sprite = PND.Spr("art/obstacles/wind_particle");
            wind.emissionRectExtents = new Vector2(54f, 2f);
            wind.direction = Vector2.down;      // (0, 1) no Godot
            wind.spreadDegrees = 6f;
            wind.gravity = Vector2.zero;
            wind.speedMin = 260f;
            wind.speedMax = 320f;
            wind.scaleMin = 0.5f;
            wind.scaleMax = 0.9f;
            wind.color = new Color(0.75f, 0.95f, 1f, 0.75f);
            wind.sortingOrder = order + 1;
        }

        static void HangingWire(string name, Transform root, Vector3 pos, int order)
        {
            var go = PND.Node(name, root, pos);
            var wire = go.AddComponent<PNDHangingWire>();

            var visual = PND.Node("Visual", go.transform, PND.G(0, -140));
            wire.visual = visual.transform;

            // AnimatedSprite2D com centered = false na posição (-16, 0): canto superior esquerdo
            var f0 = PND.Spr("art/obstacles/hanging_wire_0");
            float w = f0 != null ? f0.rect.width : 32f, h = f0 != null ? f0.rect.height : 356f;
            const string O = "art/obstacles/";
            var anim = PNDSpriteAnimator.Create("AnimatedSprite2D", visual.transform, PND.G(-16 + w * 0.5f, h * 0.5f), order,
                new PNDAnimation("spark", 12f, true,
                    (O + "hanging_wire_1", 1f), (O + "hanging_wire_0", 1f), (O + "hanging_wire_2", 1f), (O + "hanging_wire_0", 2f),
                    (O + "hanging_wire_3", 1f), (O + "hanging_wire_1", 1f), (O + "hanging_wire_0", 3f)));
            wire.anim = anim;

            // Sparks (CPUParticles2D)
            var sparks = PND.Node("Sparks", visual.transform, PND.G(0, 342)).AddComponent<PNDParticles>();
            sparks.amount = 10;
            sparks.lifetime = 0.45f;
            sparks.explosiveness = 0.2f;
            sparks.direction = Vector2.down;
            sparks.spreadDegrees = 75f;
            sparks.gravity = new Vector2(0f, -450f); // (0, 450) no Godot
            sparks.speedMin = 30f;
            sparks.speedMax = 85f;
            sparks.scaleMin = 1f;
            sparks.scaleMax = 2f;
            sparks.color = new Color(1f, 0.88f, 0.3f, 1f);
            sparks.sortingOrder = order + 1;
            wire.sparks = sparks;

            Box(go, new Vector2(12, 340), 0, 30);
            wire.zapSound = PND.Audio(go, "audio/sfx/zap1");
        }
    }

    /// <summary>
    /// Gerador do modo infinito: sorteia obstáculos à frente do jogador, apaga os que
    /// ficaram para trás e mantém o chão sempre embaixo dele.
    ///
    /// Regras para nunca ficar impossível:
    ///  - cada obstáculo tem uma "zona" (esquerda/direita) e as zonas nunca encostam;
    ///  - o espaço livre entre duas zonas é medido em TEMPO de corrida (GapMin..GapMax segundos
    ///    na velocidade daquele trecho; o intervalo encolhe com a distância, mas nunca abaixo de 1 s), então cresce junto com a velocidade: sempre dá
    ///    para aterrissar do pulo anterior e reagir ao próximo;
    ///  - o mesmo tipo nunca sai duas vezes seguidas;
    ///  - os tipos mais difíceis só entram depois de alguns obstáculos;
    ///  - mais adiante aparecem COMBOS (pular + deslizar em sequência curta): o espaço é menor,
    ///    mas ainda garante aterrissar antes do fio / levantar antes do pulo.
    /// </summary>
    public class PNDSpawner : MonoBehaviour
    {
        public Transform root;
        public PNDPlayer player;
        public Transform ground;

        struct Kind
        {
            public string type;
            public float y;           // altura (igual à da fase original)
            public float left, right; // zona ocupada para cada lado do centro
            public int weight;        // chance de ser sorteado
            public int minIndex;      // só aparece a partir do N-ésimo obstáculo
            public bool combo;        // obstáculo pequeno de pular: pode fazer dupla com o fio pendurado

            public Kind(string type, float y, float left, float right, int weight, int minIndex, bool combo = false)
            {
                this.type = type; this.y = y; this.left = left; this.right = right;
                this.weight = weight; this.minIndex = minIndex; this.combo = combo;
            }
        }

        static readonly Kind[] Kinds =
        {
            new Kind("papers",  224,  28,  28, 3, 0, true),
            new Kind("pencils", 225,  26,  26, 3, 0, true),
            new Kind("wires",   218,  27,  27, 3, 0, true),
            new Kind("nbobs",   216,  25,  25, 3, 0, true),
            new Kind("cooler",  194,  34,  46, 3, 1),
            new Kind("hwire",     0,  10,  10, 3, 2), // precisa deslizar
            new Kind("ac",       20,  64,  64, 1, 3), // vento: só deixa lento
            new Kind("cleaner", 202, 106, 106, 2, 5), // patrulha 80 px para cada lado
        };

        const float FirstX = 520f;         // onde começa o primeiro obstáculo
        const float SpawnAhead = 1100f;    // cria com folga, fora da tela (a tela mostra ~470 px à frente)
        const float DespawnBehind = 520f;  // apaga depois que saiu da tela
        // Espaço livre entre obstáculos, em segundos de corrida. Vai encolhendo com a pressão.
        // (Um pulo dura 0,82 s: nunca use menos de 0,9.)
        const float GapMinEasy = 1.25f, GapMaxEasy = 2.1f; // começo da corrida
        const float GapMinHard = 1.0f,  GapMaxHard = 1.4f; // dificuldade máxima

        // Combos pular + deslizar (ou deslizar + pular)
        const int HangingWire = 5;         // posição do "hwire" na lista Kinds
        const int ComboFromIndex = 12;     // só depois de tantos obstáculos
        const float ComboChanceEasy = 0.3f;  // chance de um obstáculo simples abrir um combo
        const float ComboChanceHard = 0.6f;
        const float TripleChanceHard = 0.5f; // chance de o combo ter 3 obstáculos (0 no começo)
        // Espaço dentro do combo, em segundos de corrida. Um pulo dura 0,82 s, então com
        // 0,95 s o jogador sempre já aterrissou antes do fio (basta segurar ↓ ainda no ar).
        const float ComboGapMin = 0.95f;
        const float ComboGapMax = 1.1f;

        float nextEdge = FirstX; // a partir de onde a zona do próximo obstáculo pode começar
        int lastKind = -1;
        int forcedKind = -1; // próximo obstáculo de um combo em andamento
        int chainLeft;       // quantos obstáculos ainda faltam no combo
        int count;
        readonly Queue<(GameObject go, float endX)> alive = new Queue<(GameObject go, float endX)>();

        void Update()
        {
            if (player == null || root == null) return;
            float px = player.transform.position.x;

            while (nextEdge < px + SpawnAhead)
                SpawnNext();

            while (alive.Count > 0 && alive.Peek().endX < px - DespawnBehind)
            {
                var old = alive.Dequeue();
                if (old.go != null) Destroy(old.go);
            }

            // Chão infinito: reposiciona em passos de 128 px (o desenho do piso não "pula")
            if (ground != null && Mathf.Abs(px - ground.position.x) > 1024f)
            {
                var g = ground.position;
                g.x = Mathf.Round(px / 128f) * 128f;
                ground.position = g;
            }
        }

        void SpawnNext()
        {
            bool comboSecond = forcedKind >= 0;
            int k = comboSecond ? forcedKind : Pick();
            forcedKind = -1;
            var kind = Kinds[k];
            float x = nextEdge + kind.left;
            float endX = x + kind.right;

            var holder = PND.Node("Obstacle" + count + "_" + kind.type, root, Vector3.zero);
            PNDLevel.Spawn(kind.type, holder.transform, PND.G(x, kind.y), player, 10 + (count % 900) * 5);
            alive.Enqueue((holder, endX));

            // Espaço livre até o próximo, proporcional à velocidade que o jogador terá ali
            float speed = PNDLevel.SpeedAt(endX - PNDLevel.PlayerStartX);
            float pressure = PNDLevel.Pressure(endX - PNDLevel.PlayerStartX);

            if (chainLeft == 0)
            {
                // Abre um combo novo? (pulo → fio, fio → pulo e, mais adiante, sequências de três)
                bool canCombo = !comboSecond && count >= ComboFromIndex && (kind.combo || k == HangingWire);
                if (canCombo && Random.value < Mathf.Lerp(ComboChanceEasy, ComboChanceHard, pressure))
                    chainLeft = Random.value < TripleChanceHard * pressure ? 2 : 1;
            }

            if (chainLeft > 0)
            {
                // Dentro do combo sempre alterna pular / deslizar, com espaço curto
                chainLeft--;
                forcedKind = k == HangingWire ? PickCombo() : HangingWire;
                nextEdge = endX + speed * Random.Range(ComboGapMin, ComboGapMax);
            }
            else
            {
                float gapMin = Mathf.Lerp(GapMinEasy, GapMinHard, pressure);
                float gapMax = Mathf.Lerp(GapMaxEasy, GapMaxHard, pressure);
                nextEdge = endX + speed * Random.Range(gapMin, gapMax);
            }

            lastKind = k;
            count++;
        }

        /// <summary>Sorteia um dos obstáculos pequenos de pular.</summary>
        int PickCombo()
        {
            int n = 0;
            for (int i = 0; i < Kinds.Length; i++)
                if (Kinds[i].combo) n++;
            int r = Random.Range(0, n);
            for (int i = 0; i < Kinds.Length; i++)
            {
                if (!Kinds[i].combo) continue;
                if (r-- == 0) return i;
            }
            return 0;
        }

        bool Allowed(int i)
        {
            return i != lastKind && count >= Kinds[i].minIndex;
        }

        int Pick()
        {
            int total = 0;
            for (int i = 0; i < Kinds.Length; i++)
                if (Allowed(i)) total += Kinds[i].weight;

            int r = Random.Range(0, total);
            for (int i = 0; i < Kinds.Length; i++)
            {
                if (!Allowed(i)) continue;
                r -= Kinds[i].weight;
                if (r < 0) return i;
            }
            return 0;
        }
    }

    /// <summary>
    /// Camera2D do Player (offset (150, -50), drag horizontal com margem direita 0)
    /// + ParallaxBackground (motion_scale (0.5, 0.4), motion_mirroring 688).
    /// </summary>
    public class PNDCameraFollow : MonoBehaviour
    {
        public Camera cam;
        public Transform target;
        public SpriteRenderer[] backgrounds;

        const float HalfW = 320f, HalfH = 180f;
        static readonly Vector2 Offset = new Vector2(150f, -50f); // em coordenadas Godot
        const float DragLeft = 0.2f, DragRight = 0f;
        static readonly Vector2 MotionScale = new Vector2(0.5f, 0.4f);
        const float Mirroring = 688f;

        float camX;
        bool init;

        void LateUpdate()
        {
            if (cam == null || target == null) return;

            // Coordenadas no "espaço Godot" (Y para baixo)
            float tx = target.position.x;
            float ty = -target.position.y;

            if (!init)
            {
                camX = tx;
                init = true;
            }

            // Drag horizontal: a câmera só anda quando o player passa do centro (margem direita 0)
            // e volta se ele ficar mais de 20% de meia tela para trás.
            if (tx > camX + DragRight * HalfW) camX = tx - DragRight * HalfW;
            else if (tx < camX - DragLeft * HalfW) camX = tx + DragLeft * HalfW;

            float centerX = camX + Offset.x;
            float centerY = ty + Offset.y; // vertical sem drag: segue o player direto

            cam.transform.position = new Vector3(centerX, -centerY, -10f);

            // ----- Parallax -----
            if (backgrounds == null || backgrounds.Length == 0) return;
            float topLeftX = centerX - HalfW;
            float topLeftY = centerY - HalfH;
            float layerX = Mathf.Floor(-topLeftX) * MotionScale.x;
            float layerY = Mathf.Floor(-topLeftY) * MotionScale.y;
            layerX -= Mirroring * Mathf.Ceil(layerX / Mirroring);

            for (int i = 0; i < backgrounds.Length; i++)
            {
                var sr = backgrounds[i];
                if (sr == null || sr.sprite == null) continue;
                float w = sr.sprite.rect.width * 0.5f, h = sr.sprite.rect.height * 0.58f;
                // Sprite (centered = false) em (0, -110) dentro da camada, repetido a cada 688 px
                float sx = layerX + i * Mirroring;
                float sy = layerY - 110f;
                float gx = topLeftX + sx + w * 0.5f;
                float gy = topLeftY + sy + h * 0.5f;
                sr.transform.position = new Vector3(gx, -gy, 0f);
            }
        }
    }
}
