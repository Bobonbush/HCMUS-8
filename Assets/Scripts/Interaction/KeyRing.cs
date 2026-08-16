using System.Collections.Generic;
using UnityEngine;

namespace FreeStyle
{
    /// <summary>The character's set of keys. A locked Door asks this before opening.</summary>
    [AddComponentMenu("FreeStyle/Interaction/Key Ring")]
    public class KeyRing : MonoBehaviour
    {
        [Tooltip("Keys the character already owns at the start of the game (handy for testing).")]
        [SerializeField] List<string> startingKeys = new List<string>();

        readonly HashSet<string> _keys = new HashSet<string>();

        void Awake()
        {
            foreach (var key in startingKeys)
                if (!string.IsNullOrWhiteSpace(key)) _keys.Add(key.Trim());
        }

        public bool Has(string keyId) =>
            !string.IsNullOrWhiteSpace(keyId) && _keys.Contains(keyId.Trim());

        public void Add(string keyId)
        {
            if (!string.IsNullOrWhiteSpace(keyId)) _keys.Add(keyId.Trim());
        }

        public bool Remove(string keyId) =>
            !string.IsNullOrWhiteSpace(keyId) && _keys.Remove(keyId.Trim());

        public IEnumerable<string> All => _keys;
    }
}
