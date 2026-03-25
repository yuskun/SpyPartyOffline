using UnityEngine;

[CreateAssetMenu(fileName = "CharacterAvatarData", menuName = "CharacterAvatarData/CharacterAvatarData")]
public class CharacterAvatarData : ScriptableObject
{
    [System.Serializable]
    public class CharacterEntry
    {
        public string characterName;
        public Sprite avatarSprite;
    }

    public CharacterEntry[] characters;

    public Sprite GetAvatar(int index)
    {
        if (index >= 0 && index < characters.Length)
            return characters[index].avatarSprite;
        return null;
    }
}
