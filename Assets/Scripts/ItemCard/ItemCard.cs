
using UnityEngine;
[CreateAssetMenu(menuName = "Card/ItemCard")]
public class ItemCard : Card
{
    public GameObject itemPrefab;
   
    public virtual void SpwanPrefab(Transform transform)
    {
       ObjectSpawner.Instance.objectToSpawn(itemPrefab, transform);
    }


}
