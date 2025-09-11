using UnityEngine;

public class WallTile : MonoBehaviour {
    public Coord a;
    public Coord b;

    public void SetState(string newState) {
        if (newState == "destroyed") {
            gameObject.SetActive(false);
        } else if (newState == "damaged") {
            GetComponent<Renderer>().material.color = Color.red;
        } else {
            GetComponent<Renderer>().material.color = Color.white;
        }
    }

    public bool Matches(Coord ca, Coord cb) {
        return (a.row == ca.row && a.col == ca.col && b.row == cb.row && b.col == cb.col)
            || (a.row == cb.row && a.col == cb.col && b.row == ca.row && b.col == ca.col);
    }
}
