using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

/** This class will build the map select menu out of the LevelIcon prefabs
      using the MapAtlas information on the Gameplaymanager prefab.
    The Map Select will go inside the level icon cotnainer, filling out the
      page as best it can, which will then be centered by unity's canvas. */
public class MapSelectBuilder : MonoBehaviour
{
    
    public GameObject gameplayManagerPrefab;
    public GameObject levelIconPrefab;
    public RectTransform levelIconContainer;

    public int maxPageWidth = 3; // How many icons can we have on the w/h
    public int maxPageHeight = 2;

    private int page;
    private List<GameObject> menuElements;

    public void ReloadMenu() 
    {
        List<GameObject> newMenuElements = new List<GameObject>();

        int levelCount = Enum.GetValues(typeof(KartLevel)).Length;
        int iconsOnPage = Math.Min(MaxPageSize, levelCount-(page*MaxPageSize));

        for(int i = 0; i < iconsOnPage; i++) {
            int enumIdx = page*MaxPageSize + i;

            GameObject lvlIconObj = null;
            // Check if we can reuse an old icon obj, if not spawn a new one
            if(menuElements != null && menuElements.Count > 0) {
                lvlIconObj = menuElements[0];
                menuElements.RemoveAt(0);
            } else {
                lvlIconObj = Instantiate(levelIconPrefab, levelIconContainer);
            }

            newMenuElements.Add(lvlIconObj);

            // Set attributes
            LevelDataPackage data = gameplayManagerPrefab.GetComponent<LevelAtlas>().RetrieveData((KartLevel)enumIdx);
            lvlIconObj.GetComponent<LevelIcon>().Load(data);
            
        }

        // Delete old menu elements
        menuElements?.ForEach(e => Destroy(e));

        this.menuElements = newMenuElements;
    }

    public void NextPage() { page++; if(page >= PageCount) page = 0; }
    public void PrevPage() { page--; if(page < 0) page = PageCount-1; }
    public void SetPage(int page) { this.page = Math.Clamp(0, PageCount, page); }

    public int PageCount { get { 
        int lvlCount = gameplayManagerPrefab.GetComponent<LevelAtlas>().Levels.Count;
        return (int)(lvlCount/(float)MaxPageSize) + 1; 
    } }

    public int MaxPageSize { get { return maxPageWidth*maxPageHeight; } }

    public List<GameObject> MenuElements { get { return menuElements; } }

}