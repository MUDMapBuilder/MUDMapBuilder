# About
[![Discord](https://img.shields.io/discord/1335801517856264264)](https://discord.gg/BNSrPSgkYc)

![image](https://github.com/user-attachments/assets/4e6bbb60-717d-439b-9a5d-d8574be32c1b)

MUDMapBuilder is utility to generate map images for MUDs(Multi-User Dungeons) areas.

It is intended to be used by MUD owners, who want to add the cartography section to their sites with maps for their areas.

If you would like to see samples of MUDMapBuilder work, then check https://mudmapbuilder.github.io/

It has maps(generated with the MUDMapBuilder) and eqlist of various open source MUD codebases.

# Installation
Download the binary release(mmb.v.v.v.v.zip from the latest release at [Releases](https://github.com/rds1983/MUDMapBuilder/releases)). 

For now, it works only under Windows.

# Usage of mmb
The utility is used like this: `mmb Midgaard.json Midgaard.png`

The input json contains the area data and looks like this: https://mudmapbuilder.github.io/data/tbaMUD/maps/json/Southern%20Midgaard.json

That json was used to generate the image at the beginning of this document.

# Usage of mmb-bc
`mmb-bc` is utility to generate all maps for jsons in the specified folder

It is used like this: `mmb-bc maps/json maps/png`

# Support
https://discord.gg/BNSrPSgkYc
