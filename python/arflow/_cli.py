"""ARFlow command line interface."""

import argparse
import logging
import os
from pathlib import Path
from typing import Any, Sequence

from arflow._core import ARFlowServicer, run_server
from arflow._replay import ARFlowPlayer


def _validate_dir_path(path_as_str: str | None) -> str | None:
    """Check if the path is a valid directory."""
    if path_as_str is None:
        return None
    if not os.path.isdir(path_as_str):
        raise argparse.ArgumentTypeError(f"{path_as_str} is not a valid path.")
    return path_as_str


def _validate_file_path(path_as_str: str) -> str:
    """Check if the path is a valid file."""
    if not os.path.isfile(path_as_str):
        raise argparse.ArgumentTypeError(f"{path_as_str} is not a valid file.")
    return path_as_str


def serve(args: Any):
    """Run the ARFlow server."""
    run_server(
        ARFlowServicer,
        port=args.port,
        path_to_save=Path(args.save_path) if args.save_path else None,
    )


def replay(args: Any):
    """Replay an ARFlow data file."""
    player = ARFlowPlayer(ARFlowServicer, Path(args.file_path))
    player.run()


def parse_args(
    argv: Sequence[str] | None = None,
) -> tuple[argparse.ArgumentParser, argparse.Namespace]:
    parser = argparse.ArgumentParser(description="ARFlow CLI")
    subparsers = parser.add_subparsers()

    parser.add_argument(
        "-d",
        "--debug",
        action="store_true",
        help="Enable debug mode",
    )

    # Serve subcommand
    serve_parser = subparsers.add_parser("serve", help="Run a simple ARFlow server")
    serve_parser.add_argument(
        "-p",
        "--port",
        type=int,
        default=8500,
        help="Port to run the server on.",
    )
    serve_parser.add_argument(
        "-s",
        "--save_path",
        type=_validate_dir_path,
        default=None,
        help="Path to the directory to save the requests history. If not provided, the requests history will not be saved.",
    )
    serve_parser.set_defaults(func=serve)

    # Replay subcommand
    replay_parser = subparsers.add_parser("replay", help="Replay an ARFlow data file")
    replay_parser.add_argument(
        "file_path",
        type=_validate_file_path,
        help="Path to the ARFlow data file.",
    )
    replay_parser.set_defaults(func=replay)

    parsed_args = parser.parse_args(argv)

    if parsed_args.debug:
        logging.getLogger().setLevel(logging.DEBUG)

    return parser, parsed_args


def main(argv: Sequence[str] | None = None):  # pragma: no cover
    parser, args = parse_args(argv)
    if hasattr(args, "func"):
        args.func(args)
    else:
        parser.print_help()


if __name__ == "__main__":  # pragma: no cover
    logging.basicConfig()  # TODO: Replace print with logging
    main()