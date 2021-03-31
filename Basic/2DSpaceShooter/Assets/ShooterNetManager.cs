// using UnityEngine;
// using System.Collections.Generic;
// using UnityEngine.Networking;
//
// public class ShooterNetManager : NetworkManager {
//
// 	public GameObject bulletPrefab;
// 	
// 	public int BulletsInPool = 50;
// 	public int BulletsToStart = 5;
// 	public List<GameObject> bullets;
// 	//
// 	// void AddBulletToPool()
// 	// {
// 	// 	var bullet = Instantiate(bulletPrefab, Vector3.zero, Quaternion.identity);
// 	// 	bullet.SetActive(false);
// 	// 	DontDestroyOnLoad(bullet);
// 	// 	bullets.Add(bullet);
// 	// }
// 	
// 	void OnLevelLoaded()
// 	{
// 		for (int i=0; i < BulletsInPool; i++)
// 		{
// 			bullets[i].SetActive(false);
// 		}
// 		
// 	}
// 	
// 	// int FindFreeBulletIndex()
// 	// {
// 	// 	for (int i=0; i < BulletsInPool; i++)
// 	// 	{
// 	// 		if (i >= bullets.Count)
// 	// 		{
// 	// 			AddBulletToPool();
// 	// 		}
// 	// 		if (bullets[i].activeSelf == false)
// 	// 		{
// 	// 			return i;
// 	// 		}
// 	// 	}
// 	// 	return -1;
// 	// }
// 	//
// 	
// 	// public GameObject OnSpawnBullet(Vector3 position, string assetId)
// 	// {
// 	// 	int bulletIndex = FindFreeBulletIndex();
// 	// 	if (bulletIndex == -1)
// 	// 	{
// 	// 		Debug.LogError("no more bullets");
// 	// 		return null;
// 	// 	}
// 	// 	
// 	// 	GameObject newBullet = bullets[bulletIndex];
// 	// 	newBullet.transform.position = position;
// 	// 	newBullet.SetActive(true);
// 	// 	return newBullet;
// 	// }
// 		
// 	public void OnUnSpawnBullet(GameObject spawned)
// 	{
// 		spawned.SetActive(false);
// 	}
// 	
// }
