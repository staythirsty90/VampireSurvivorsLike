using UnityEngine;

public class CharacterSelect : MonoBehaviour {
    Character character;

    /// <summary>
    /// Get the Selected Character.
    /// </summary>
    public Character GetCharacter() {
        return character;
    }
    /// <summary>
    /// Set the Selected Character.
    /// </summary>
    public void SetCharacter(Character value) {
        character = value;
    }
}