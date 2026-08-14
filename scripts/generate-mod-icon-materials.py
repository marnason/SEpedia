#!/usr/bin/env python3
"""Generate transparent materials for inclusion in an icon-owning source mod."""

from __future__ import print_function

import argparse
import hashlib
import io
import os
import re
import sys
import xml.etree.ElementTree as ET


def normalize(path):
    return path.strip().replace("/", "\\")


def collect_icons(mod_root):
    data_root = os.path.join(mod_root, "Data")
    if not os.path.isdir(data_root):
        raise SystemExit("Mod Data directory was not found: {0}".format(data_root))

    icons = {}
    parse_failures = []
    for directory, _, files in os.walk(data_root):
        for filename in files:
            if not filename.lower().endswith(".sbc"):
                continue
            source_path = os.path.join(directory, filename)
            try:
                root = ET.parse(source_path).getroot()
            except (ET.ParseError, OSError) as exception:
                parse_failures.append("{0}: {1}".format(source_path, exception))
                continue

            for node in root.iter("Icon"):
                if not node.text:
                    continue
                icon = normalize(node.text)
                if not icon.lower().endswith((".dds", ".png")):
                    continue
                if os.path.isabs(icon) or icon.startswith("\\") or ".." in icon.split("\\"):
                    continue
                source_icon = os.path.join(mod_root, *icon.split("\\"))
                if not os.path.isfile(source_icon):
                    continue
                icons.setdefault(icon.lower(), icon)

    for failure in parse_failures:
        print("warning: could not parse {0}".format(failure), file=sys.stderr)
    return sorted(icons.values(), key=lambda value: value.lower())


def safe_namespace(value):
    result = re.sub(r"[^A-Za-z0-9_]", "_", value.strip())
    if not result:
        raise SystemExit("Material namespace must contain at least one letter, number, or underscore.")
    return result


def material_id(namespace, icon):
    digest = hashlib.sha256(icon.lower().encode("utf-8")).hexdigest()[:16]
    return "SEpediaIcon_{0}_{1}".format(namespace, digest)


def build_materials(namespace, icons):
    definitions = ET.Element(
        "Definitions",
        {
            "xmlns:xsi": "http://www.w3.org/2001/XMLSchema-instance",
            "xmlns:xsd": "http://www.w3.org/2001/XMLSchema",
        },
    )
    materials = ET.SubElement(definitions, "TransparentMaterials")
    ids = set()
    for icon in icons:
        subtype = material_id(namespace, icon)
        if subtype.lower() in ids:
            raise SystemExit("Generated duplicate material ID: {0}".format(subtype))
        ids.add(subtype.lower())

        material = ET.SubElement(materials, "TransparentMaterial")
        identifier = ET.SubElement(material, "Id")
        ET.SubElement(identifier, "TypeId").text = "TransparentMaterialDefinition"
        ET.SubElement(identifier, "SubtypeId").text = subtype
        ET.SubElement(material, "AlphaMistingEnable").text = "false"
        ET.SubElement(material, "CanBeAffectedByOtherLights").text = "false"
        ET.SubElement(material, "SoftParticleDistanceScale").text = "0"
        ET.SubElement(material, "Texture").text = icon
        ET.SubElement(material, "Reflectivity").text = "0"

    ET.indent(definitions, space="  ")
    output = io.BytesIO()
    ET.ElementTree(definitions).write(output, encoding="utf-8", xml_declaration=True)
    return output.getvalue()


def main():
    parser = argparse.ArgumentParser(
        description="Generate an SBC that must be placed inside the icon-owning source mod."
    )
    parser.add_argument("mod_root", help="Root directory of the source mod")
    parser.add_argument("namespace", help="Globally unique material namespace, usually the mod ID")
    parser.add_argument("output", help="Generated .sbc output path")
    parser.add_argument("--check", action="store_true", help="Fail if the existing output is stale")
    arguments = parser.parse_args()

    mod_root = os.path.abspath(arguments.mod_root)
    namespace = safe_namespace(arguments.namespace)
    icons = collect_icons(mod_root)
    if not icons:
        raise SystemExit("No mod-owned definition icon textures were found.")

    generated = build_materials(namespace, icons)
    output_path = os.path.abspath(arguments.output)
    if arguments.check:
        try:
            with open(output_path, "rb") as existing_file:
                existing = existing_file.read()
        except OSError:
            raise SystemExit("Generated material file is missing: {0}".format(output_path))
        if existing != generated:
            raise SystemExit("Generated material file is stale: {0}".format(output_path))
    else:
        with open(output_path, "wb") as output_file:
            output_file.write(generated)
    print("Generated {0} mod icon materials.".format(len(icons)))


if __name__ == "__main__":
    main()
