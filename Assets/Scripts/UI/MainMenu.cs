using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour, IPointerEnterHandler
{
    private string game = "SampleScene";
    [SerializeField] private GameObject main;
    [SerializeField] private GameObject load;

    private void Awake() {
        StartCoroutine(PlayMenuTheme());
    }

    private IEnumerator PlayMenuTheme(){
        yield return new WaitForSeconds(0.5f);
        SoundManager.LoopMusic(SoundManager.Sound.Theme_menu);
    }

    
    public void StartGame(){
        ButtonSfx();
        SceneManager.LoadScene(game);
    }

    public void OpenLoad(){
        ButtonSfx();
        load.SetActive(true);
        main.SetActive(false);
    }

    public void CloseLoad(){
        ButtonSfx();
        load.SetActive(false);
        main.SetActive(true);
    }

    public void QuitGame(){
        ButtonSfx();
        Application.Quit();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
         SoundManager.PlaySound(SoundManager.Sound.ButtonHover);
    }

    public void ButtonSfx(){
        SoundManager.PlaySound(SoundManager.Sound.ButtonPressed);
    }
}
