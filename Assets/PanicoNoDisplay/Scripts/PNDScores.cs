using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

namespace PanicoNoDisplay
{
    [Serializable]
    public class PNDScoreEntry
    {
        public string name;
        public int score;
    }

    [Serializable]
    class PNDScoreFile
    {
        public List<PNDScoreEntry> entries = new List<PNDScoreEntry>();
    }

    /// <summary>
    /// Ranking do minijogo (TOP 5). Fica salvo em um arquivo JSON na pasta de dados do jogo
    /// (Application.persistentDataPath), então continua lá depois de fechar o jogo.
    /// O nome do jogador NÃO é lembrado: cada pessoa digita o seu ao entrar no ranking.
    /// </summary>
    public static class PNDScores
    {
        public const int TopCount = 5;
        public const int MaxNameLength = 10;
        const string FileName = "panico_no_display_ranking.json";

        static List<PNDScoreEntry> cache;

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>Lista ordenada do maior para o menor score (no máximo TopCount itens).</summary>
        public static List<PNDScoreEntry> Top
        {
            get
            {
                if (cache == null) Load();
                return cache;
            }
        }

        public static int Best => Top.Count > 0 ? Top[0].score : 0;

        /// <summary>O score entra no TOP 5?</summary>
        public static bool Qualifies(int score)
        {
            if (score <= 0) return false;
            return Top.Count < TopCount || score > Top[Top.Count - 1].score;
        }

        /// <summary>Adiciona o score e devolve a posição (0 = primeiro) ou -1 se não entrou.</summary>
        public static int Add(string name, int score)
        {
            var list = Top;
            name = string.IsNullOrWhiteSpace(name) ? "JOGADOR" : name.Trim();
            if (name.Length > MaxNameLength) name = name.Substring(0, MaxNameLength);

            // Em caso de empate, quem fez primeiro fica na frente
            int index = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                if (score > list[i].score)
                {
                    index = i;
                    break;
                }
            }
            if (index >= TopCount) return -1;

            list.Insert(index, new PNDScoreEntry { name = name, score = score });
            while (list.Count > TopCount) list.RemoveAt(list.Count - 1);
            Save();
            return index;
        }

        /// <summary>Apaga o ranking inteiro.</summary>
        public static void Clear()
        {
            cache = new List<PNDScoreEntry>();
            Save();
        }

        static void Load()
        {
            cache = new List<PNDScoreEntry>();
            try
            {
                if (!File.Exists(FilePath)) return;
                var data = JsonUtility.FromJson<PNDScoreFile>(File.ReadAllText(FilePath));
                if (data == null || data.entries == null) return;
                foreach (var e in data.entries)
                {
                    if (e == null || e.score <= 0) continue;
                    if (string.IsNullOrWhiteSpace(e.name)) e.name = "JOGADOR";
                    cache.Add(e);
                }
                cache.Sort((a, b) => b.score.CompareTo(a.score));
                while (cache.Count > TopCount) cache.RemoveAt(cache.Count - 1);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Pânico no Display] Não foi possível ler o ranking: " + ex.Message);
            }
        }

        static void Save()
        {
            try
            {
                var data = new PNDScoreFile { entries = cache };
                File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Pânico no Display] Não foi possível salvar o ranking: " + ex.Message);
            }
        }

        // ---------------- Quadro do TOP 5 (menu e tela de fim) ----------------

        static readonly PNDStyle BoardStyle = new PNDStyle { bg = new Color(0.05f, 0.07f, 0.14f, 0.85f), radius = 4 }
            .Border(new Color(1f, 0.8f, 0.2f, 0.9f), 1);

        /// <summary>Monta o quadro com os 5 melhores. 'highlight' destaca uma posição (o score recém-salvo).</summary>
        public static RectTransform BuildBoard(Transform parent, int highlight = -1)
        {
            var panel = PNDUI.StyledBox("Ranking", parent, BoardStyle, false);
            var rt = panel.rectTransform;

            var box = PNDUI.G(PNDUI.Rect("VBox", rt), 0, 0, 1, 1, 12, 8, -12, -8);
            PNDUI.VBox(box, 3f);

            PNDUI.Label("Title", box, "TOP " + TopCount, 16, new Color(1f, 0.8f, 0.2f), TextAlignmentOptions.Center, 4, Color.black);

            var list = Top;
            for (int i = 0; i < TopCount; i++)
            {
                bool has = i < list.Count;
                Color c = i == highlight ? new Color(0.3f, 1f, 0.3f)
                    : has ? new Color(0.92f, 0.92f, 0.95f)
                    : new Color(0.5f, 0.5f, 0.55f);

                var row = PNDUI.Rect("Row" + i, box);
                PNDUI.HBox(row, 6f, TextAnchor.MiddleLeft);

                var pos = PNDUI.Label("Pos", row, (i + 1) + ".", 14, c);
                PNDUI.Layout(pos, 18, -1, 18, -1);
                var name = PNDUI.Label("Name", row, has ? list[i].name : "---", 14, c);
                PNDUI.Layout(name, flexW: 1);
                PNDUI.Label("Score", row, has ? list[i].score.ToString() : "", 14, c, TextAlignmentOptions.Right);
            }
            return rt;
        }
    }
}
