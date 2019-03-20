package enumerated;

import java.util.Arrays;
import java.util.HashSet;

/**
 * CharacterCategory
 */
public enum CharacterCategory {

    VOWEL('a', 'e', 'i', 'o', 'u') {
        public String toString() {
            return "vowel";
        }
    },
    SOMETIMES_A_VOWEL('y', 'w') {
        public String toString() {
            return "sometimes a vowel";
        }
    },
    CONSONANT {
        @Override
        public String toString() {
            return "consonant";
        }
    };
    private HashSet<Character> chars = new HashSet<>();

    private CharacterCategory(Character... chars) {
        if (chars != null)
            this.chars.addAll(Arrays.asList(chars));
    }

    public static CharacterCategory getCategory(Character c) {
        if (VOWEL.chars.contains(c))
            return VOWEL;
        if (SOMETIMES_A_VOWEL.chars.contains(c))
            return CharacterCategory.SOMETIMES_A_VOWEL;
        return CONSONANT;
    }
}