### **Advocate requires that you install [RePak](https://github.com/r-ex/RePak/releases) to function**

# Advocate Skin Converter

This tool's main purpose is to aid skin creators in transitioning towards using rpak-based Northstar mods as the primary way to distribute skins.

## Setup

1. Download [RePak](https://github.com/r-ex/RePak/releases)
2. Download and run [Advocate](https://github.com/ASpoonPlaysGames/Advocate/releases/latest)
3. Open the Settings in Advocate

![image](https://user-images.githubusercontent.com/66967891/190265203-60eb5920-bba4-47bd-beae-3ed855d7cd81.png)

4. Set the RePak Path so that it points to RePak.exe

![image](https://user-images.githubusercontent.com/66967891/190265432-36054dbd-d5bf-48f2-92ff-307a4cd4eb8b.png)

5. Set the Output Path so that it points to a folder. **This is where the skins will be put when they are converted**

![image](https://user-images.githubusercontent.com/66967891/190265456-154cb78e-dba5-4fec-aeb0-e325eae3360f.png)

6. (Optional) Edit your Description Template. Click on the ? for details on how to use Description Tags.

![image](https://user-images.githubusercontent.com/66967891/202273770-c1cf5e5c-21bd-4b50-a0fb-197abeaaae69.png)


## Usage - UI

1. Create your skin using the Titanfall 2 Skin Tool
2. Set the Skin Path so that it points to the skin's .zip file

![image](https://user-images.githubusercontent.com/66967891/190265672-6466bef0-0bf7-4bf8-bcb6-01969e48af33.png)

3. (Optional) Set the README Path so that it points to a .md file. This will be used as a description for your mod when you upload it to Thunderstore

![image](https://user-images.githubusercontent.com/66967891/190265874-2ef601c6-384d-4022-90ec-fcb2876ea213.png)

4. (Optional) Set the Icon path so that it points to a .png file that is 256x256

![image](https://user-images.githubusercontent.com/66967891/190265885-d692cb17-c9e3-4b7b-b66d-0246d939640c.png)

5. Set the Author Name and Skin Name

![image](https://user-images.githubusercontent.com/66967891/190266008-1c4938ef-6ba3-4d14-b39e-879c45fdb042.png)

6. Set the Version in the format `MAJOR.MINOR.PATCH` for example: `1.0.0`

![image](https://user-images.githubusercontent.com/66967891/190266330-67fb86ea-e3f0-4a80-8f9b-4fd2172e9d05.png)

7. Check the message next to the "Convert Skin(s)" for any problems

![image](https://user-images.githubusercontent.com/66967891/190266349-2845bda2-3255-4112-bf05-f6ef353087cb.png)

8. Press the "Convert Skin(s)" button if there are no problems

![image](https://user-images.githubusercontent.com/66967891/190266363-160282d9-c9b2-4ccb-b0ad-c5d8c6a272ff.png)

9. Find your newly-created mod in your output folder.

## Usage - CLI

Advocate now has a CLI! Below is a table of the different commandline arguments that you can use.

| Argument       | Optional?          | Description                                            | Example                                                |
| -------------- | ------------------ | ------------------------------------------------------ | ------------------------------------------------------ |
| `-nogui`       | :heavy_check_mark: | Makes Advocate launch without the gui.                 | `-nogui`                                               |
| `-author`      | :x:                | Sets the Author of the skin.                           | `"-author=Spoon"`                                      |
| `-name`        | :x:                | Sets the Name of the skin.                             | `"-name=TestCLISkin"`                                  |
| `-version`     | :x:                | Sets the Version of the skin.                          | `"-version=1.0.0"`                                     |
| `-outputpath`  | :x:                | Sets the output directory for the skin.                | `"-outputpath=C:/Users/Spoon/Documents/Advocate"`      |
| `-repakpath`   | :x:                | Sets the file path where Advocate will look for RePak. | `"-repakpath=C:/Users/Spoon/Documents/RePak.exe"`      |
| `-desc`        | :x:                | Sets the description template for the skin.            | `"-desc={AUTHOR} made this cool skin for the {TYPES}"` |



