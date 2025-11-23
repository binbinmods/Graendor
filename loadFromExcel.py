import os
import json
import pandas as pd
import pathlib 
import numpy as np
import ast

# Set the directory where the Excel file is located
excel_file_name = "Graendor.xlsx"
character_name = "Graendor"
config = f"{character_name}/BepInEx/config/Obeliskial_importing/{character_name}"

# This sets up the names of the directories for later.
script_dir = os.path.dirname(__file__)
excel_dir = script_dir
excel_file_path = os.path.join(excel_dir,excel_file_name)
config_dir = os.path.join(script_dir,config)


# This is the sheets info from the spreadsheet. Do not touch
sheet_list = ['BinbinSubclass', 'BinbinPack', 'BinbinGameObject', 'BinbinSkin', 'BinbinCardback']
# sheet_list = ['BinbinSubclass'] 
sheet_map = {'BinbinSubclass':"subclass", 'BinbinPack':"pack", 'BinbinGameObject':"gameObject", 'BinbinSkin':"skin", 'BinbinCardback':"cardback"}


def parse_cell_value(value):
    '''
    Handles dicts/lists in the excel file
    '''

    if isinstance(value, (list, dict)):
        return value
    
    # removes na
    if pd.isna(value):
        return ""
    
    str_value = str(value).strip()
    
    if str_value.startswith('[') and str_value.endswith(']'):
        try:
            parsed = ast.literal_eval(str_value)
            return parsed
        except (ValueError, SyntaxError):
            return str_value
    
    if str_value.startswith('{') and str_value.endswith('}'):
        try:
            parsed = ast.literal_eval(str_value)
            return parsed
        except (ValueError, SyntaxError):
            return str_value
    
    return value


def xls_to_excelfile()->pd.ExcelFile:
    # print("xls_to_excelfile")
    xls:pd.ExcelFile = pd.ExcelFile(excel_file_path)

    # print(xls.parse("Visuals"))
    return xls

def convert_xls_sheet_to_json(xls:pd.ExcelFile, sheet_name:str, output_directory):
    # output_directory = script_dir

    df = pd.read_excel(xls, sheet_name=sheet_name)
    # df = df.replace({np.nan: ""})

    # Parse it all
    df = df.map(parse_cell_value)
        
    # Convert to dicts to import
    data = df.to_dict(orient='records')[0]
    
    if sheet_name == "BinbinSubclass":
        character_name=df["CharacterName"][0].replace(" ", "")

    # json_filename = sheet_name 
    json_filename = get_json_filename(df,sheet_name)
    json_file_path = os.path.join(output_directory, json_filename)

    os.makedirs(output_directory, exist_ok=True)
    
    # Makes json
    with open(json_file_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=4)
    

def get_json_filename(df:pd.DataFrame, sheet_name:str):
    if sheet_name=="BinbinSubclass":
        name = df["ID"][0]
        return f"{name}.json"
    if sheet_name=="BinbinPack":
        name= df["PackID"][0]
        return f"{name}.json"
    if sheet_name=="BinbinGameObject":
        return f"skin{character_name}.json"
    if sheet_name=="BinbinSkin":
        name = df["SkinID"][0]
        return f"{name}.json"
    if sheet_name=="BinbinCardback":
        name = df["CardbackID"][0]
        return f"{name}.json"
    
    raise ValueError("Incorrect SheetName")

def handle_traits(xls:pd.ExcelFile):
    trait_dir = os.path.join(script_dir, f"{config_dir}/trait")
    df = pd.read_excel(xls, sheet_name="BinbinTraits")
    df = df.replace({np.nan: ""})

    os.makedirs(trait_dir, exist_ok=True)

    # Loads the source json that will be modified
    with open(os.path.join(script_dir,'traitSource.json'), 'r') as file:
        data = json.load(file)

    vars_to_set = ["ID","TraitName","Description","Activation","TraitCard","TimesPerTurn"]
    for index, row in df.iterrows():
        for var in vars_to_set:
            if var == "Activation" and row[var] == "":
                row[var] = "None"
            data[var] = row[var]
    
        # json_filename = sheet_name 
        id = data["ID"]
        json_filename = f"{id}.json"
        json_file_path = os.path.join(trait_dir, json_filename)
        
        

        # Makes json
        with open(json_file_path, 'w', encoding='utf-8') as f:            
            json.dump(data, f, ensure_ascii=False, indent=4)

    

if __name__=="__main__":
    xls = xls_to_excelfile()

    os.makedirs(os.path.join(config_dir, "card"), exist_ok=True)
    os.makedirs(os.path.join(config_dir, "sprite"), exist_ok=True)

    for sheet,sheet_dir in sheet_map.items():
        print(f"Handling {sheet}")
        output_dir = os.path.join(config_dir,sheet_dir)
        convert_xls_sheet_to_json(xls,sheet,output_dir)
        print(f"Handled {sheet}")

    handle_traits(xls)

    print("Completed")
