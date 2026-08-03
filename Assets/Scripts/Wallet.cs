using UnityEngine;
public class Wallet : MonoBehaviour
{
public static Wallet instance;
public int currentGold = 0;
void Awake()
    {
        if(instance == null)
        {
          instance = this;
          DontDestroyOnLoad(gameObject);  
        }
        else
        {
            Destroy(gameObject);
        }
    }
  public void AddGold(int amount)
    {
        currentGold += Mathf.Max(0, amount);
    }
    public bool SpendGold(int amount)
    {
        if(amount > currentGold) return false;
        currentGold -= amount;
        return true;
    }
    public void SetGold(int amount)
    {
        currentGold = Mathf.Max(0, amount);
    }
}
