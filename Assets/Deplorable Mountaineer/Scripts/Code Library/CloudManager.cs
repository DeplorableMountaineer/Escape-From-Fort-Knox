using System;
using System.Collections;
using System.Collections.Generic;
using Deplorable_Mountaineer.Singleton;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Deplorable_Mountaineer.Code_Library {
    public class CloudManager : PersistentSingleton<CloudManager> {
        private readonly List<LeaderboardEntry> _leaderboard = new List<LeaderboardEntry>();
        private const string Url = "http://dreamlo.com/lb/";
        private const string PlayerNameKey = "PLAYER_NAME";

        private const string CloudKey =
            "Deplorable Mountaineer Cloud Leaderboard Key for Framework 2D correct horse battery staple";

        [SerializeField] private string key = "";

        /// <summary>
        /// In Start() below, uncomment the ShowEncryptedKey(); line
        /// 
        /// Then, put the private key obtained from dreamlo.com into the "key" field
        /// in the inspector.
        ///
        /// Run the code so "start()" runs and copy the encrypted key from the console.
        ///
        /// Pasted the encrypted key into the value below.
        ///
        /// Then remove the private key from the "key" field in the inspector.  Take care
        /// not to allow the private key to be stored on a public Git or other source
        /// control system.
        ///
        /// Finally, comment the ShowEncryptedKey(); line.
        ///
        /// Store the private key in a safe place so the leaderboard can be edited later.
        /// 
        /// </summary>
        private const string EncryptedKey =
            "EAAAAH/+1e8vpfk1vTtYYqBCwTQZvjbcoj9r2UsGMFxF815piMW2iuStpKPMqOc4kbVrsqmc7VPAVQeDnzOMdWcZCjw=";

        [SerializeField] private string keyPub = "";

        private string _playerName = GameUtils.GenericPlayerName;

        public string PlayerName => _playerName;

        [UsedImplicitly]
        private void ShowEncryptedKey(){
            Debug.Log($"Key = [{key}]");
            string eKey = Crypto.EncryptStringAes(key, CloudKey, false);
            Debug.Log($"Encrypted = [{eKey}]");
            string dKey = Crypto.DecryptStringAes(eKey, CloudKey, false);
            Debug.Log($"Decrypted = [{dKey}]");
        }

        private void Start(){
            UpdateLeaderboardMenu();

            //uncomment this to encrypt a new key
            //ShowEncryptedKey();
            if(string.IsNullOrWhiteSpace(key))
                key = Crypto.DecryptStringAes(EncryptedKey, CloudKey, false);

            _playerName = Crypto.HasKey(PlayerNameKey)
                ? Crypto.GetString(PlayerNameKey)
                : GameUtils.GuessPlayerName();
            StartCoroutine(LoadLeaderboardFromCloud());
        }

        private string SanitizePlayerName(string newName){
            int countSmallLetters = 0;
            string result = newName;
            result = string.IsNullOrWhiteSpace(result) ? GameUtils.GuessPlayerName() : result;
            result = result.Replace("/", "").Replace("|", "").Replace("*", "");
            string tmp = result;
            result = "";
            char lastC = 'A';
            foreach(char c in tmp){
                if(lastC == c && char.IsPunctuation(c))
                    continue;
                lastC = c;
                if(char.IsControl(c)) continue;
                if(char.IsSymbol(c)) continue;
                if(char.IsWhiteSpace(c) || c == '_'){
                    result += ' ';
                    continue;
                }

                if(char.IsLower(c)) countSmallLetters++;
                result += c;
            }

            if(countSmallLetters < 1) result = result.ToLower();

            while(result.Contains("  "))
                result = result.Replace("  ", " ");

            if(result.Length > 25) result = result.Substring(0, 25);
            if(string.IsNullOrWhiteSpace(result)) result = GameUtils.GenericPlayerName;
            return result;
        }

        public string SetNewPlayerName(string newName){
            _playerName = SanitizePlayerName(newName);
            Crypto.SetString(PlayerNameKey, _playerName);
            return _playerName;
        }

        public void ResetHighScores(){
            string request = $"{Url}<private key>/clear";
            Debug.Log($"run {request} in browser to clear leaderboard");
        }

        public void AddNewHighScore(int score = 0, bool isTurbo = false){
           // if(score < GameManager.Instance.HighScore) return;
            string turbo = isTurbo ? "Turbo" : "";
            string n = UnityWebRequest.EscapeURL(PlayerName);
            string request = $"{Url}{key}/add/{n}/{score}/1000/{turbo}";
            StartCoroutine(SubmitHighScore(request));
        }

        private IEnumerator SubmitHighScore(string request){
            for(int i = 0; i < 12; i++){
                using UnityWebRequest www = UnityWebRequest.Get(request);
                yield return www.SendWebRequest();
                if(string.IsNullOrWhiteSpace(www.error)){
                   // UiManager.Instance.DisplayMessage("High score submitted to cloud");
                    yield return LoadLeaderboardFromCloud();
                    yield break;
                }

                float time = Mathf.Max(Mathf.Pow(10, i*.5f), 10);
                if(time < 100) time = Mathf.Ceil(time/10)*10;
                else if(time < 1000) time = Mathf.Ceil(time/100)*100;
                else time = Mathf.Ceil(time/1000)*1000;
              //  UiManager.Instance.DisplayMessage(
                  //  $"Unable to store to leaderboard in the cloud: {www.error};" +
                  //  $" retrying in {time} seconds");
                yield return new WaitForSecondsRealtime(time);
            }
        }

        /// <summary>
        /// 
        /// 
        /// Rated R for strong language
        /// 
        /// 
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="currentPlayerOk"></param>
        /// <returns></returns>
        private bool
            IsSuspiciousLeaderboardEntry(string[] fields, bool currentPlayerOk = true){
            if(fields.Length < 6) return true;
            string lbName = UnityWebRequest.UnEscapeURL(fields[0]);
            lbName = SanitizePlayerName(lbName);
            if(currentPlayerOk && lbName == _playerName) return false;
            if(!long.TryParse(fields[1], out long score)) return true;
            if(fields[2] != "1000") return true;
            if(score > int.MaxValue || score < 0) return true;
            if(fields[3] != "Turbo" && !string.IsNullOrWhiteSpace(fields[3])) return true;

            string[] badWords = {
                "ass", "shit", "damn", "fuck", "hell", "bastard", "bitch",
                "whore", "turd", "pussy", "cunt", "piss", "sex", "sexy", "anal",
                "asshole", "fucker", "asswipe", "dickhead", "dickwad", "shitter",
                "dammit", "bitching", "bitches", "asses", "shits", "bastards",
                "whores", "whorehouse", "turds", "pussies", "cunts", "pissing",
                "pisser", "pisses", "pissed", "sexed", "assholes", "fuckers",
                "asswipes", "dickheads", "dickwads", "shitters", "pissers", "teabagger",
                "teabagging", "asshat", "asshats", "teabaggers", "porn", "pron",
                "barf", "barfing", "beastiality", "sodomy", "sucks", "barelylegal",
                "naked", "nekkid", "nude", "biatch", "bi", "fag", "gay", "homo",
                "lez", "lesbian", "gays", "lesbians", "biteme", "cock", "cocksucker",
                "cocks", "cocksuckers", "blowjob", "boobs", "boob", "boobies",
                "sideboob", "underboob", "cleavage", "booty", "brea5t", "brea5ts",
                "braless", "bugger", "buggery", "bullshit", "dyke", "trans", "tranny",
                "crossdresser", "buttfuck", "buttfucker", "rimmer", "buttplug",
                "cameltoe", "cameltoes", "chink", "nigger", "chinks", "niggers",
                "spic", "spics", "kike", "kikes", "clit", "felatio", "cocktease",
                "cum", "cybersex", "nigga", "niggas", "cracka", "crackas",
                "deepthroat", "dipshit", "dragqueen", "dumbass", "faggot",
                "faggots", "fags", "threesome", "foursome", "jihad", "nazi",
                "retard", "semen", "skank", "bsdm", "dom", "sub", "slut",
                "spank", "suicide", "tit", "tits", "deplorable", "deplorablemountaineer"
            };

            foreach(string word in badWords){
                int i = lbName.IndexOf(word, StringComparison.OrdinalIgnoreCase);
                if(i >= 0) Debug.Log($"Bad word detected: {i} {lbName} {word}");
                if(i < 0) continue;
                if(i > 0 && char.IsLetter(lbName[i - 1])) continue;
                if(i + word.Length < lbName.Length
                   && char.IsLetter(lbName[i + word.Length])) continue;
                return true;
            }

            return false;
        }

        private IEnumerator LoadLeaderboardFromCloud(){
            string webResult;
            string error;
            _leaderboard.Clear();
            using(UnityWebRequest www = UnityWebRequest.Get(Url + keyPub + "/pipe")){
                yield return www.SendWebRequest();
                webResult = www.downloadHandler.text;
                error = www.error;
            }

            if(string.IsNullOrEmpty(webResult)){
                yield return new WaitForSeconds(.5f);
                if(!string.IsNullOrEmpty(error))
                    // UiManager.Instance.DisplayMessage(
                    //     $"Unable to retrieve leaderboard from cloud: {error}");
                yield break;
            }

            string[] records = webResult.Split(new[] {'\n'},
                StringSplitOptions.RemoveEmptyEntries);
            if(records.Length == 0) yield break;
            int index = 0;
            while(index < 10){
                if(index >= records.Length) break;
                string[] fields = records[index].Split('|');
                index++;
#if UNITY_EDITOR
                if(IsSuspiciousLeaderboardEntry(fields, false)){
                    if(fields.Length < 6)
                        Debug.Log("Leaderboard entry has wrong number of fields");
                    if(fields.Length > 0){
                        string result =
                            SanitizePlayerName(UnityWebRequest.UnEscapeURL(fields[0]));
                        for(int i = 1; i < fields.Length; i++)
                            result += "   <" + fields[i] + ">";
                        Debug.Log("Suspicious leaderboard entry detected: "
                                  + result);
                    }
                }
#endif
                if(IsSuspiciousLeaderboardEntry(fields)) continue;
                LeaderboardEntry lbe = new LeaderboardEntry {
                    name = SanitizePlayerName(UnityWebRequest.UnEscapeURL(fields[0])),
                    score = fields.Length > 1 ? ParseInt(fields[1]) : 0,
                    isTurbo = fields.Length > 3 && fields[3] == "Turbo"
                };
                _leaderboard.Add(lbe);
            }

            _leaderboard.Sort();
            _leaderboard.Reverse();
            UpdateLeaderboardMenu();
        }

        public void UpdateLeaderboardMenu(){
            Transform p = GameUtils.FindComponentByTag<Transform>("Leaderboard Panel");
            if(!p) return;
            for(int index = 0; index < p.childCount; index++){
                Transform scorePanel = p.GetChild(index);
                TMP_Text t;
                Image im;
                if(index < _leaderboard.Count){
                    t = GameUtils.FindComponentByTag<TMP_Text>("Leaderboard Rank Text",
                        scorePanel);
                    if(t) t.text = (index + 1).ToString();

                    t = GameUtils.FindComponentByTag<TMP_Text>("Leaderboard Name Text",
                        scorePanel);
                    if(t) t.text = _leaderboard[index].name;
                    t = GameUtils.FindComponentByTag<TMP_Text>("Leaderboard Score Text",
                        scorePanel);
                    if(t) t.text = _leaderboard[index].score.ToString();
                    im = GameUtils.FindComponentByTag<Image>(
                        "Leaderboard Turbo Indicator",
                        scorePanel);
                    if(im){
                        Color c = im.color;
                        c.a = _leaderboard[index].isTurbo ? 1 : 0;
                        im.color = c;
                    }

                    continue;
                }

                t = GameUtils.FindComponentByTag<TMP_Text>("Leaderboard Rank Text",
                    scorePanel);
                if(t) t.text = "";

                t = GameUtils.FindComponentByTag<TMP_Text>("Leaderboard Name Text",
                    scorePanel);
                if(t) t.text = "";
                t = GameUtils.FindComponentByTag<TMP_Text>("Leaderboard Score Text",
                    scorePanel);
                if(t) t.text = "";
                im = GameUtils.FindComponentByTag<Image>(
                    "Leaderboard Turbo Indicator",
                    scorePanel);
                if(im){
                    Color c = im.color;
                    c.a = 0;
                    im.color = c;
                }
            }
        }

        private int ParseInt(string s){
            int.TryParse(s, out int result);
            return result;
        }
    }

    [Serializable]
    public class LeaderboardEntry : IComparable<LeaderboardEntry>, IComparable {
        public string name;
        public int score;
        public bool isTurbo = false;

        public int CompareTo(LeaderboardEntry other){
            if(ReferenceEquals(this, other)) return 0;
            return ReferenceEquals(null, other) ? 1 : score.CompareTo(other.score);
        }

        public int CompareTo(object obj){
            if(ReferenceEquals(null, obj)) return 1;
            if(ReferenceEquals(this, obj)) return 0;
            return obj is LeaderboardEntry other
                ? CompareTo(other)
                : throw new ArgumentException(
                    $"Object must be of type {nameof(LeaderboardEntry)}");
        }
    }
}