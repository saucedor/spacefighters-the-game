using UnityEngine;

public class WallTile : MonoBehaviour {
    public int rowA, colA;
    public int rowB, colB;

    // Estados posibles: intacto, dañado, destruido
    public string state = "intact";

    public void SetState(string newState) {
        state = newState;

        if (state == "destroyed") {
            gameObject.SetActive(false); // desaparecer muro
        }
        else if (state == "damaged") {
            GetComponent<Renderer>().material.color = Color.red; // por ejemplo
        }
        else {
            GetComponent<Renderer>().material.color = Color.white; // intacto
        }
    }

    public bool Matches(int[] a, int[] b) {
        // Compara extremos sin importar el orden
        return (rowA == a[0] && colA == a[1] && rowB == b[0] && colB == b[1]) ||
               (rowA == b[0] && colA == b[1] && rowB == a[0] && colB == a[1]);
    }
}
