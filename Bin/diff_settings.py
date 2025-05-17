def apply(config, args):
    config["arch"] = "i686"
    config["expected_dir"] = "expected/" # needed for -o
    config["objdump_executable"] = "objdump"
