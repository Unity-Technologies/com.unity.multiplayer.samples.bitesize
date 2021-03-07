using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Tanks.Utilities;

namespace Tanks.Rules
{
    [CreateAssetMenu(fileName = "KillLogPhrases", menuName = "KillLog/Phrases", order = 1)]
    public class KillLogPhrases : ScriptableObject
    {
        [SerializeField]
        protected List<string> m_KillPhrases, m_SuicidePhrases;

        public string GetRandomKillPhrase(string killerName, Color killerColor, string victimName, Color victimColor)
        {
            string killPhrase = GetRandomKillPhraseSyntax();
            killPhrase = killPhrase.Replace("[killer]", StringBuilding.GetColorisedPlayerName(killerName, killerColor));
            killPhrase = killPhrase.Replace("[victim]", StringBuilding.GetColorisedPlayerName(victimName, victimColor));
            return killPhrase;
        }

        public string GetRandomSuicidePhrase(string victimName, Color victimColor)
        {
            string suicidePhrase = GetRandomSuicidePhraseSyntax();
            suicidePhrase = suicidePhrase.Replace("[victim]", StringBuilding.GetColorisedPlayerName(victimName, victimColor));
            return suicidePhrase;
        }

        private string GetRandomKillPhraseSyntax()
        {
            if (m_KillPhrases.Count == 0)
            {
                return "[killer] killed [victim]";
            }
            
            return m_KillPhrases[Random.Range(0, m_KillPhrases.Count)];
        }

        private string GetRandomSuicidePhraseSyntax()
        {
            if (m_SuicidePhrases.Count == 0)
            {
                return "[victim] committed suicide";
            }
            
            return m_SuicidePhrases[Random.Range(0, m_SuicidePhrases.Count)];
        }

    }
}