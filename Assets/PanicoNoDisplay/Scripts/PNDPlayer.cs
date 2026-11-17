using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PanicoNoDisplay
{
    /// <summary>
    /// Conversão de scripts/player/player.gd (CharacterBody2D).
    /// Corre sozinho para a direita, pula (ESPAÇO / ↑), desliza (↓ segurando),
    /// fica lento no vento do ar-condicionado e, ao bater em um obstáculo,
    /// cai/escorrega/tropeça e a corrida acaba (modo infinito, sem vidas).
    /// A velocidade aumenta com a distância percorrida (PNDLevel.SpeedAt).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class PNDPlayer : MonoBehaviour
    {
        public const float BASE_SPEED = 200f;
        public const float JUMP_VELOCITY = 400f; // Godot: -400 (Y para baixo)

        public float runSpeed = BASE_SPEED;   // velocidade da corrida (cresce com a distância)
        public float currentSpeed = BASE_SPEED; // velocidade real (com o vento do ar-condicionado)
        public float gravity = PND.GodotGravity;

        public bool isSliding;
        float slideExitTime; // quadro "levantando" ao sair do deslize
        public bool isHurt; // bateu: está caindo e a corrida vai acabar
        string hurtAnimName = "hurt";
        float activeSlowdown = 1f;

        public Vector2 velocity; // em coordenadas Unity (Y para cima)

        public PNDHud hud;
        public PNDSpriteAnimator anim;
        public AudioSource jumpSound;
        public AudioSource hitSound;

        float startX;
        /// <summary>Distância percorrida desde o início da corrida (em pixels).</summary>
        public float Distance => transform.position.x - startX;

        BoxCollider2D col;
        Rigidbody2D rb;
        bool onFloor;

        static readonly Vector2 SizeNormal = new Vector2(32f, 48f);
        static readonly Vector2 SizeSlide = new Vector2(32f, 24f);
        const float Skin = 0.05f;

        readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
        ContactFilter2D solidFilter;

        void Awake()
        {
            col = GetComponent<BoxCollider2D>();
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            col.size = SizeNormal;
            col.offset = Vector2.zero;

            solidFilter = new ContactFilter2D();
            solidFilter.useTriggers = false;
            solidFilter.useLayerMask = false;

            startX = transform.position.x;
        }

        void FixedUpdate()
        {
            float delta = Time.fixedDeltaTime;

            // 1. Aplica gravidade
            velocity.y -= gravity * delta;

            if (isHurt)
            {
                MoveAndSlide(delta);
                return; // Sai cedo se estiver machucado (não obedece controles)
            }

            var kb = Keyboard.current;

            // 2. Pulo
            if (kb != null && (kb.spaceKey.isPressed || kb.upArrowKey.isPressed))
            {
                if (onFloor && !isSliding)
                {
                    velocity.y = JUMP_VELOCITY;
                    if (jumpSound != null) jumpSound.Play();
                }
            }

            // 3. Deslize
            if (kb != null && kb.downArrowKey.isPressed)
            {
                if (onFloor && !isSliding)
                    StartSlide();
            }
            else
            {
                if (isSliding)
                    StopSlide();
            }

            // 4. Movimento: a corrida acelera conforme a distância
            runSpeed = PNDLevel.SpeedAt(Distance);
            currentSpeed = runSpeed * activeSlowdown;
            velocity.x = currentSpeed;
            MoveAndSlide(delta);

            // 5. GERENCIADOR DE ANIMAÇÕES
            if (isHurt)
            {
                if (anim.Current != hurtAnimName)
                    anim.Play(hurtAnimName);
            }
            else if (isSliding)
            {
                if (anim.Current != "slide") // não reinicia (a animação não repete)
                    anim.Play("slide");
            }
            else if (slideExitTime > 0f && onFloor)
            {
                slideExitTime -= delta;
                anim.Play("slide_end");
            }
            else if (!onFloor)
            {
                anim.Play("jump");
            }
            else
            {
                anim.Play("run");
            }
        }

        // ---------------- move_and_slide simplificado ----------------
        // Move separadamente em X e Y, parando ao encostar em colisores sólidos
        // (o chão). Áreas (obstáculos) são triggers.
        void MoveAndSlide(float delta)
        {
            Vector2 pos = rb.position;

            float dx = velocity.x * delta;
            if (dx != 0f)
            {
                float allowed = Cast(pos, new Vector2(Mathf.Sign(dx), 0f), Mathf.Abs(dx), out bool hitX);
                if (hitX)
                {
                    dx = Mathf.Sign(dx) * allowed;
                    velocity.x = 0f;
                }
                pos.x += dx;
            }

            onFloor = false;
            float dy = velocity.y * delta;
            if (dy != 0f)
            {
                float allowed = Cast(pos, new Vector2(0f, Mathf.Sign(dy)), Mathf.Abs(dy), out bool hitY);
                if (hitY)
                {
                    if (dy < 0f) onFloor = true;
                    dy = Mathf.Sign(dy) * allowed;
                    velocity.y = 0f;
                }
                pos.y += dy;
            }

            rb.MovePosition(pos);
        }

        float Cast(Vector2 pos, Vector2 dir, float dist, out bool hit)
        {
            hit = false;
            Vector2 center = pos + col.offset;
            hits.Clear();
            Physics2D.BoxCast(center, col.size, 0f, dir, solidFilter, hits, dist + Skin);
            float best = dist;
            foreach (var h in hits)
            {
                if (h.collider == null || h.collider == col || h.collider.isTrigger) continue;
                if (h.collider.attachedRigidbody == rb) continue;
                float d = Mathf.Max(0f, h.distance - Skin);
                if (d <= best)
                {
                    best = d;
                    hit = true;
                }
            }
            return best;
        }

        public bool IsOnFloor => onFloor;

        public void StartSlide()
        {
            isSliding = true;
            col.size = SizeSlide;
            col.offset = new Vector2(0f, -12f); // Godot: position.y = 12
            // A arte 'player_slide' já é desenhada encostada no chão
            anim.transform.localPosition = Vector3.zero;
        }

        public void StopSlide()
        {
            isSliding = false;
            slideExitTime = 0.12f;
            col.size = SizeNormal;
            col.offset = Vector2.zero;
            // Volta a imagem para a altura normal
            anim.transform.localPosition = Vector3.zero;
        }

        /// <summary>Bateu em um obstáculo: cai e a corrida acaba (não há vidas).</summary>
        public void TakeDamage(string damageType = "hurt", string reason = null)
        {
            if (isHurt) return; // Já bateu

            if (isSliding)
                StopSlide();

            isHurt = true;
            if (hitSound != null) hitSound.Play();
            hurtAnimName = anim.Has(damageType) ? damageType : "hurt";
            anim.Play(hurtAnimName);

            if (damageType == "slip")
            {
                // Escorrega um pouco para frente com os pés para o alto e cai no chão
                velocity.x = 90f;
                velocity.y = 140f;
                anim.transform.localPosition = Vector3.zero;
            }
            else if (damageType == "trip")
            {
                // Tropeça para frente e cai de cara no chão
                velocity.x = 60f;
                velocity.y = 90f;
                anim.transform.localPosition = Vector3.zero;
            }
            else
            {
                velocity.x = 0f;
            }

            currentSpeed = 0f; // Para de correr

            if (string.IsNullOrEmpty(reason))
                reason = damageType == "slip" ? "Você escorregou!"
                    : damageType == "trip" ? "Você tropeçou!"
                    : "Você bateu!";

            StartCoroutine(DeathRoutine(reason));
        }

        // Mostra a queda por um instante e então abre a tela de fim de jogo.
        IEnumerator DeathRoutine(string reason)
        {
            // Após o impulso inicial do escorregão/tropeço, para no chão
            yield return new WaitForSecondsRealtime(0.25f);
            velocity.x = 0f;

            // Pisca em vermelho enquanto está caído
            for (int i = 0; i < 3; i++)
            {
                PND.SetTint(anim.sr, new Color(1f, 0.2f, 0.2f)); // Vermelho
                yield return new WaitForSecondsRealtime(0.12f);
                PND.SetTint(anim.sr, Color.white); // Normal
                yield return new WaitForSecondsRealtime(0.12f);
            }

            if (hud != null)
                hud.ShowGameOver(reason);
        }

        public void ApplySlowdown(float multiplier)
        {
            activeSlowdown = multiplier;
            if (!isHurt)
                PND.SetTint(anim.sr, new Color(0.5f, 0.8f, 1f)); // Personagem azul/congelado
        }

        public void RemoveSlowdown()
        {
            activeSlowdown = 1f;
            if (!isHurt)
                PND.SetTint(anim.sr, Color.white); // Cor normal
        }

        // ---------------- Montagem (player.tscn) ----------------
        public static PNDPlayer Create(Transform parent, Vector3 pos, PNDHud hud, int order)
        {
            var go = new GameObject("Player");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = SizeNormal;

            const string H = "art/player/hero/";
            var anim = PNDSpriteAnimator.Create("AnimatedSprite2D", go.transform, Vector3.zero, order,
                new PNDAnimation("idle", 7f, true,
                    (H + "player_idle1", 1f), (H + "player_idle2", 1f), (H + "player_idle3", 1f), (H + "player_idle4", 1f),
                    (H + "player_idle5", 1f), (H + "player_idle4", 1f), (H + "player_idle3", 1f), (H + "player_idle2", 1f)),
                new PNDAnimation("run", 12f, true,
                    (H + "player_run1", 1f), (H + "player_run2", 1f), (H + "player_run3", 1f), (H + "player_run4", 1f), (H + "player_run5", 1f)),
                new PNDAnimation("jump", 5f, true, (H + "player_jump", 1f)),
                new PNDAnimation("slide", 8f, false, (H + "player_slide_1", 1f), (H + "player_slide", 1f)),
                new PNDAnimation("slide_end", 5f, false, (H + "player_slide_3", 1f)),
                new PNDAnimation("hurt", 5f, true, (H + "player_hurt", 1f)),
                new PNDAnimation("slip", 9f, false,
                    (H + "player_slip_1", 1f), (H + "player_slip_2", 1f), (H + "player_slip_3", 1.5f), (H + "player_slip_4", 6f)),
                new PNDAnimation("trip", 9f, false,
                    (H + "player_trip_1", 1f), (H + "player_trip_2", 1f), (H + "player_trip_3", 1.5f), (H + "player_trip_4", 6f)));
            // Personagem novo: sprites 128x128 com o dobro de detalhe, mostrados em escala 0.5
            // (os pés ficam na linha 112 da imagem = base da colisão 32x48)
            anim.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            anim.Play("run"); // autoplay = "run"

            var p = go.AddComponent<PNDPlayer>();
            p.anim = anim;
            p.hud = hud;
            p.jumpSound = PND.Audio(go, "audio/sfx/phaseJump1");
            p.hitSound = PND.Audio(go, "audio/sfx/impactMetal_heavy_000", -4f);
            return p;
        }
    }
}
