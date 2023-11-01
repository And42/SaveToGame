from pathlib import Path
import shutil
import zipfile
import subprocess


def run_command(command: str, cwd: Path):
    subprocess.Popen(command, cwd=cwd).wait()


def delete_dir(dir_path: Path):
    shutil.rmtree(dir_path, ignore_errors=True)


def unpack_zip(zip_file_path: Path, output_dir: Path):
    with zipfile.ZipFile(zip_file_path, "r") as zip_ref:
        zip_ref.extractall(output_dir)


def unpack_zip_dir(zip_file_path: Path, dir_name: str, output_dir: Path):
    with zipfile.ZipFile(zip_file_path, "r") as zip_ref:
        unpack_dir(zip_ref, dir_name, output_dir)


def unpack_dir(zip_file: zipfile.ZipFile, dir_name: str, output_dir: Path):
    actual_name = dir_name if dir_name.endswith("/") else dir_name + "/"

    for item in zip_file.namelist():
        if item.startswith(actual_name):
            zip_file.extract(member=item, path=output_dir / item.removeprefix(actual_name))


def add_directory_to_zip(zip_file: zipfile.ZipFile, dir_path: Path):
    for item in dir_path.rglob("*"):
        if item.is_file():
            zip_file.write(item, arcname=Path(dir_path.name) / item.relative_to(dir_path).as_posix())


def copy_file(source: Path, destination: Path):
    shutil.copyfile(source, destination)


def copy_file_to_dir(source: Path, destination_dir: Path):
    copy_file(source, destination_dir / source.name)


step_counter = 0
substep_counter = 0


def print_step(name: str):
    globals()["step_counter"] += 1
    print("")
    print(f"--- {step_counter}. {name} ---")
    print("")
    globals()["substep_counter"] = 0


def print_substep(name: str):
    globals()["substep_counter"] += 1
    print(f"  - {step_counter}.{substep_counter}. {name} -")
    print("")


if __name__ == "__main__":
    step_counter = 0

    dotnet_path = Path("dotnet")
    inno_script_path = Path("iscc")

    ci_root = Path(__file__).parent.parent
    solution_root = ci_root.parent
    project_cs_path = solution_root / "SaveToGame" / "SaveToGame.csproj"
    compressed_resources_zip = solution_root / "compiled_app_resources.zip"
    inno_script_script_path = solution_root / "SaveToGame.iss"

    app_version = project_cs_path.read_text(encoding="utf-8").split("<Version>")[1].split("</Version>")[0].strip()
    app_short_version = ".".join(app_version.split(".")[:2])

    building_dir = ci_root / "Building"
    save_to_game_publish_dir = building_dir / "SaveToGame publish"
    inno_script_building_path = building_dir / inno_script_script_path.name
    decompressed_resources_dir = building_dir / "Decompressed resources"
    inno_script_output_dir = building_dir / "Inno script output"
    artifacts_dir = building_dir / "Artifacts"
    portable_variant_artifact = artifacts_dir / f"SaveToGame_{app_short_version}_portable.zip"

    print_step("Clean up")
    delete_dir(building_dir)

    print_step("Build and test")
    run_command(f"\"{dotnet_path}\" restore", cwd=solution_root)
    run_command(f"\"{dotnet_path}\" build --no-restore", cwd=solution_root)
    run_command(f"\"{dotnet_path}\" test --no-build --verbosity normal", cwd=solution_root)

    print_step("Publish")
    run_command(
        f"\"{dotnet_path}\" "
        "publish "
        "--configuration Release "
        "--framework net6.0-windows "
        "--runtime win-x86 "
        "-p:PublishSingleFile=true "
        "--self-contained false "
        f"--output \"{save_to_game_publish_dir}\" "
        "--verbosity normal",
        cwd=solution_root
    )

    print_step("Unpack resources")
    unpack_zip(compressed_resources_zip, output_dir=decompressed_resources_dir)

    print_step("Build Inno Setup executable")
    inno_script_building_path.write_text(
        inno_script_script_path.read_text(encoding="utf-8").replace("<app_version>", app_short_version),
        encoding="utf-8"
    )
    run_command(f"\"{inno_script_path}\" \"{inno_script_building_path}\"", cwd=solution_root)

    print_step("Create artifacts")
    artifacts_dir.mkdir(exist_ok=True)

    print_substep("Copy Inno Setup executable")
    copy_file_to_dir(next(inno_script_output_dir.glob("*.exe")), artifacts_dir)

    print_substep("Create portable variant")
    with zipfile.ZipFile(portable_variant_artifact, "w", compression=zipfile.ZIP_LZMA) as zip_file:
        for item in save_to_game_publish_dir.glob("*.exe"):
            zip_file.write(item, arcname=item.name)

        for item in decompressed_resources_dir.iterdir():
            add_directory_to_zip(zip_file, item)

        zip_file.writestr("portable", data=b"")
