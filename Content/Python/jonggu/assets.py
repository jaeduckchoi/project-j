"""Canonical Unreal package paths for authored game assets.

Blueprint prefixes describe actual Blueprint asset types, including UObject data.
The migration map is a compatibility manifest, not a second authoring destination.
"""

ASSETS = {'BP_RestaurantPlayerController': '/Game/Jonggu/Core/BP_RestaurantPlayerController',
 'BP_RestaurantGameMode': '/Game/Jonggu/Core/BP_RestaurantGameMode',
 'BP_RestaurantGameInstance': '/Game/Jonggu/Core/BP_RestaurantGameInstance',
 'BP_RestaurantSaveGame': '/Game/Jonggu/Core/Save/BP_RestaurantSaveGame',
 'BP_RestaurantManager': '/Game/Jonggu/Gameplay/Restaurant/BP_RestaurantManager',
 'BP_RestaurantInteractable': '/Game/Jonggu/Gameplay/Interaction/BP_RestaurantInteractable',
 'BP_RestaurantHUD': '/Game/Jonggu/UI/Blueprints/BP_RestaurantHUD',
 'BP_PreparedDish': '/Game/Jonggu/Data/Runtime/BP_PreparedDish',
 'BP_FoodOrder': '/Game/Jonggu/Data/Runtime/BP_FoodOrder',
 'BP_RecipeDefinition': '/Game/Jonggu/Data/Recipes/BP_RecipeDefinition',
 'BP_Recipe_KimchiFriedRice': '/Game/Jonggu/Data/Recipes/BP_Recipe_KimchiFriedRice',
 'BP_Recipe_KimchiStew': '/Game/Jonggu/Data/Recipes/BP_Recipe_KimchiStew',
 'BP_Recipe_KimchiPancake': '/Game/Jonggu/Data/Recipes/BP_Recipe_KimchiPancake',
 'BP_Recipe_ClamStew': '/Game/Jonggu/Data/Recipes/BP_Recipe_ClamStew',
 'M_InteractionText': '/Game/Jonggu/Gameplay/Interaction/Materials/M_InteractionText',
 'FF_Galmuri11_Regular': '/Game/Jonggu/UI/Fonts/FF_Galmuri11_Regular',
 'FF_Galmuri11_Bold': '/Game/Jonggu/UI/Fonts/FF_Galmuri11_Bold',
 'F_Galmuri11_Regular': '/Game/Jonggu/UI/Fonts/F_Galmuri11_Regular',
 'F_Galmuri11_Bold': '/Game/Jonggu/UI/Fonts/F_Galmuri11_Bold',
 'T_UI_HUDFrame': '/Game/Jonggu/UI/Textures/T_UI_HUDFrame',
 'T_UI_HUDCard': '/Game/Jonggu/UI/Textures/T_UI_HUDCard',
 'T_UI_HUDPrompt': '/Game/Jonggu/UI/Textures/T_UI_HUDPrompt',
 'T_UI_Coin': '/Game/Jonggu/UI/Textures/T_UI_Coin',
 'T_UI_PopupFrame': '/Game/Jonggu/UI/Textures/T_UI_PopupFrame',
 'T_UI_PopupCard': '/Game/Jonggu/UI/Textures/T_UI_PopupCard',
 'T_UI_Close': '/Game/Jonggu/UI/Textures/T_UI_Close',
 'T_UI_KimchiFriedRice': '/Game/Jonggu/UI/Textures/T_UI_KimchiFriedRice',
 'T_UI_KimchiStew': '/Game/Jonggu/UI/Textures/T_UI_KimchiStew',
 'T_UI_KimchiPancake': '/Game/Jonggu/UI/Textures/T_UI_KimchiPancake'}

LEGACY_ASSET_PATHS = {'/Game/Jonggu/Prototype/BP_PrototypeController': '/Game/Jonggu/Core/BP_RestaurantPlayerController',
 '/Game/Jonggu/Prototype/BP_PrototypeGameMode': '/Game/Jonggu/Core/BP_RestaurantGameMode',
 '/Game/Jonggu/Prototype/BP_PrototypeSession': '/Game/Jonggu/Core/BP_RestaurantGameInstance',
 '/Game/Jonggu/Prototype/BP_PrototypeSave': '/Game/Jonggu/Core/Save/BP_RestaurantSaveGame',
 '/Game/Jonggu/Prototype/BP_PrototypeManager': '/Game/Jonggu/Gameplay/Restaurant/BP_RestaurantManager',
 '/Game/Jonggu/Prototype/BP_PrototypeInteractable': '/Game/Jonggu/Gameplay/Interaction/BP_RestaurantInteractable',
 '/Game/Jonggu/Prototype/BP_PrototypeHUD': '/Game/Jonggu/UI/Blueprints/BP_RestaurantHUD',
 '/Game/Jonggu/Prototype/BP_PrototypeDish': '/Game/Jonggu/Data/Runtime/BP_PreparedDish',
 '/Game/Jonggu/Prototype/BP_PrototypeOrder': '/Game/Jonggu/Data/Runtime/BP_FoodOrder',
 '/Game/Jonggu/Prototype/BP_RecipeDefinition': '/Game/Jonggu/Data/Recipes/BP_RecipeDefinition',
 '/Game/Jonggu/Prototype/BP_RecipeRice': '/Game/Jonggu/Data/Recipes/BP_Recipe_KimchiFriedRice',
 '/Game/Jonggu/Prototype/BP_RecipeSoup': '/Game/Jonggu/Data/Recipes/BP_Recipe_KimchiStew',
 '/Game/Jonggu/Prototype/BP_RecipePancake': '/Game/Jonggu/Data/Recipes/BP_Recipe_KimchiPancake',
 '/Game/Jonggu/Prototype/BP_RecipeSpecialSoup': '/Game/Jonggu/Data/Recipes/BP_Recipe_ClamStew',
 '/Game/Jonggu/Prototype/M_PrototypeInteractionText': '/Game/Jonggu/Gameplay/Interaction/Materials/M_InteractionText',
 '/Game/Jonggu/Prototype/UI/Galmuri11': '/Game/Jonggu/UI/Fonts/FF_Galmuri11_Regular',
 '/Game/Jonggu/Prototype/UI/Galmuri11_Bold': '/Game/Jonggu/UI/Fonts/FF_Galmuri11_Bold',
 '/Game/Jonggu/Prototype/UI/Galmuri11_Font': '/Game/Jonggu/UI/Fonts/F_Galmuri11_Regular',
 '/Game/Jonggu/Prototype/UI/Galmuri11_Bold_Font': '/Game/Jonggu/UI/Fonts/F_Galmuri11_Bold',
 '/Game/Jonggu/Prototype/UI/T_HUDFrame': '/Game/Jonggu/UI/Textures/T_UI_HUDFrame',
 '/Game/Jonggu/Prototype/UI/T_HUDCard': '/Game/Jonggu/UI/Textures/T_UI_HUDCard',
 '/Game/Jonggu/Prototype/UI/T_HUDPrompt': '/Game/Jonggu/UI/Textures/T_UI_HUDPrompt',
 '/Game/Jonggu/Prototype/UI/T_HUDCoin': '/Game/Jonggu/UI/Textures/T_UI_Coin',
 '/Game/Jonggu/Prototype/UI/T_PopupFrame': '/Game/Jonggu/UI/Textures/T_UI_PopupFrame',
 '/Game/Jonggu/Prototype/UI/T_PopupCard': '/Game/Jonggu/UI/Textures/T_UI_PopupCard',
 '/Game/Jonggu/Prototype/UI/T_PopupClose': '/Game/Jonggu/UI/Textures/T_UI_Close',
 '/Game/Jonggu/Prototype/UI/T_PopupFood0': '/Game/Jonggu/UI/Textures/T_UI_KimchiFriedRice',
 '/Game/Jonggu/Prototype/UI/T_PopupFood1': '/Game/Jonggu/UI/Textures/T_UI_KimchiStew',
 '/Game/Jonggu/Prototype/UI/T_PopupFood2': '/Game/Jonggu/UI/Textures/T_UI_KimchiPancake'}

def asset_path(name):
    return ASSETS[name]


def object_path(name):
    return asset_path(name) + "." + name


def generated_class_path(name):
    return object_path(name) + "_C"
