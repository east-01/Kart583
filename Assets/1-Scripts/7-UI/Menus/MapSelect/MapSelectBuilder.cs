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

    public int maxPageWidth = 3;
    public int maxPageHeight = 2;

    public float paddingH = 50;
    public float paddingV = 50;

    private int page;
    private List<GameObject> menuElements;

    public void ReloadMenu() 
    {
        List<GameObject> newMenuElements = new List<GameObject>();

        float iconW = levelIconPrefab.GetComponent<RectTransform>().rect.width;
        float iconH = levelIconPrefab.GetComponent<RectTransform>().rect.height;

        int levelCount = Enum.GetValues(typeof(KartLevel)).Length;
        int iconsOnPage = Math.Min(MaxPageSize, levelCount-(page*MaxPageSize));

        int pageWidth, pageHeight;
        (pageWidth, pageHeight) = CalculatePageDimensions(iconsOnPage);

        // Set rect transform dimensions
        float pageWidthPx = pageWidth*iconW + (pageWidth-1)*paddingH;
        float pageHeightPx = pageHeight*iconH + (pageHeight-1)*paddingV;
        levelIconContainer.sizeDelta = new Vector2(pageWidthPx, pageHeightPx);
        levelIconContainer.anchoredPosition = Vector2.zero;

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
            
            // Set location
            int x = i%maxPageWidth;
            int y = (int)(i/(float)maxPageWidth);
            lvlIconObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(x*iconW + Math.Max(0, x)*paddingH, y*iconH + Math.Max(0, y)*paddingV);

        }

        // Delete old menu elements
        menuElements?.ForEach(e => Destroy(e));

        this.menuElements = newMenuElements;
    }

    public void NextPage() { page++; if(page >= PageCount) page = 0; }
    public void PrevPage() { page--; if(page < 0) page = PageCount-1; }
    public void SetPage(int page) { this.page = Math.Clamp(0, PageCount, page); }

    /** Calculates the best page width and height given an input
          amount. We use this to lay out the level icons nicely. 
        This method is constrained to the MaxPageSize. */
    (int, int) CalculatePageDimensions(int number)
    {
        if(number <= 3) return (number, 1);
        // Ensure that we have a non-prime number
        number = FindNextNonPrime(Math.Clamp(number, 1, MaxPageSize));
        
        int factor1 = 1;
        int factor2 = number;

        // Iterate through potential factors from 2 to the square root of the number
        for (int i = 2; i <= Mathf.Sqrt(number); i++) {
            // Check if the number is divisible by the current factor
            if (number % i == 0) {
                factor1 = i;
                factor2 = number / i;
            }
        }
        return (factor1, factor2);    
    }

    int FindNextNonPrime(int number)
    {
        if (number <= 1) return 1;
        while(true) {
            for (int i = 2; i <= Mathf.Sqrt(number); i++) {
                if (number % i == 0) return number;
            }
            number++;
        }
    }

    public int PageCount { get { 
        int lvlCount = gameplayManagerPrefab.GetComponent<LevelAtlas>().Levels.Count;
        return (int)(lvlCount/(float)MaxPageSize) + 1; 
    } }

    public int MaxPageSize { get { return maxPageWidth*maxPageHeight; } }

    public List<GameObject> MenuElements { get { return menuElements; } }

}