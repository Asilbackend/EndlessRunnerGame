using UnityEngine;

public class StarsPanel : MonoBehaviour
{
    public GameObject[] stars;
    public int starsCount = 0;

    public void SetStars(int count)
    {
        starsCount = count;
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null)
                stars[i].SetActive(i < starsCount);
        }
    }
}
