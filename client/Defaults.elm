module Defaults exposing (..)

import Dict

import Types exposing (..)
import Autocomplete

escapeKeycode : Int
escapeKeycode = 27
replID : String
replID = "darkInput"
entryID : String
entryID = "darkEntry"
leftButton : Int
leftButton = 0

-- UI
nodeHeight : Int
nodeHeight = round 28
edgeGradColor : String
edgeGradColor = "#777"
edgeColor : String
edgeColor = "#444"
edgeSize : String
edgeSize = "1px"
dragEdgeStrokeColor : String
dragEdgeStrokeColor = "#444"
dragEdgeSize : String
dragEdgeSize = "1px"
initialPos : VPos
initialPos = {vx=475, vy=325}
moveSize : Int
moveSize = 50

defaultEditor : Editor
defaultEditor = {}

model2editor : Model -> Editor
model2editor m = {}

defaultModel : Editor -> Model
defaultModel e = { nodes = Dict.empty
                 , error = Nothing
                 , lastMsg = Initialization
                 , lastMod = NoChange
                 , center = {x=initialPos.vx, y=initialPos.vy}

                 -- editor TODO
                 -- these load before the graph does, causing
                 -- exceptions. We'll need to only run these after the
                 -- graph loads
                 , complete = Autocomplete.empty
                 , tempFieldName = ""
                 , state = Deselected
                 }
