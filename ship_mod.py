import os
import shutil
from pathlib import Path

def copy_directory(source_dir: str, destination_dir: str):
    source_path = Path(source_dir)
    destination_path = Path(destination_dir)
        
    destination_path.mkdir(parents=True, exist_ok=True)
    for item in source_path.iterdir():
        destination_filepath = destination_path / item.name
        if item.is_dir():
            if destination_filepath.exists():                
                shutil.rmtree(destination_filepath)
            shutil.copytree(item, destination_filepath)
        else:
            shutil.copy2(item, destination_filepath)
            
    print(f"Directory Copied")


def zip_mods():
    download_dir = os.path.expanduser("~/Downloads")
    output_name = f"{download_dir}/Mod Zips/{mod_dir}"

    print("zipping to Mod Zips")
    zip_dir = f"{script_dir}/{mod_dir}"
    shutil.make_archive(output_name, 'zip', zip_dir)

    print("zipping to local")
    output_name = f"{script_dir}/{mod_dir}"
    shutil.make_archive(output_name, 'zip', zip_dir)


if __name__ == "__main__":  
    # Name of the mod Folder in your Project.
    mod_dir = "Graendor"
    
    # Name of the "content" folder that is loaded by Obeliskial Content
    # I typically have this as the same as the mod_dir, but feel free to change them
    content_destination_dir = mod_dir
    script_dir = os.path.dirname(os.path.abspath(__file__))
    
    ## Copy to Local Game

    # source is the path to where you have your Obeliskial_importing files.
    source = f"{script_dir}/{mod_dir}/BepInEx/config/Obeliskial_importing/{mod_dir}"

    # bepinex_dir is the path to where you have your BepInEx folder. 
    # This is to load the Obeliskial_importing files locally
    bepinex_dir = os.path.abspath(os.path.join(script_dir, '..', '..', '..'))
    destination = f"{bepinex_dir}/config/Obeliskial_importing/{content_destination_dir}"    

    copy_directory(source, destination)
    
    ## Zips the mods to a local folder to upload to Thunderstore. 
    # Recommended to comment out
    zip_mods()