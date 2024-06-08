# About
![image](https://github.com/rds1983/MUDMapBuilder/assets/1057289/3f233fc8-510b-4125-a0c5-7e89d372b62e)

MUDMapBuilder is utility to generate map images for MUDs(Multi-User Dungeons) areas.

It is intended to be used by MUD owners, who want to add the cartography section to their sites with maps for their areas.

If you would like to see samples of MUDMapBuilder work, then check https://mudmapbuilder.github.io/
It has maps(generated with the MUDMapBuilder) and eqlist of various open source MUD codebases.

# Installation
Download the binary release(MUDMapBuilder.v.v.v.v.zip from the latest release at [Releases](https://github.com/rds1983/MUDMapBuilder/releases)). For now, it works only under Windows.

# Usage
The utility is used like this: `mmb Midgaard.json Midgaard.png`

The input json contains the area data and looks like this: https://mudmapbuilder.github.io/data/Envy22/maps/json/Arachnos.json

If an area has complicated structure, then the work of utility might end with following message:

`WARNING: The process wasn't completed. Try turning off fix options(fixObstacles/fixNonStraight/fixIntersected)`

In that case, try setting 'fix...' options to 'false' at "buildOptions" node in the input json. In most cases, the 'fixIntersected' is the culprit.
