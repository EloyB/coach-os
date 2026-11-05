#!/usr/bin/env python3
"""Local directory snapshots only. Archives are NOT encrypted or off-host backups."""
import argparse
import os
from pathlib import Path
import tempfile
import zipfile


def backup(source, archive, quiesced):
    import stat
    source, archive = Path(source), Path(archive)
    if source.is_symlink() or not source.is_dir():
        raise ValueError('Source must be a real directory')
    if archive.exists() or archive.is_symlink() or archive.parent.resolve().is_relative_to(source.resolve()):
        raise ValueError('Use a new archive outside the source')
    if archive.parent.stat().st_mode & 0o077:
        raise ValueError('Backup parent must be private (0700)')
    if not quiesced:
        raise ValueError('Stop all keyring writers first and explicitly confirm --quiesced')
    files = sorted(source.iterdir())
    if not files:
        raise ValueError('Empty keyring')
    fd, temporary = tempfile.mkstemp(prefix='.keyring-', dir=archive.parent)
    try:
        with os.fdopen(fd, 'wb') as output:
            with zipfile.ZipFile(output, 'w', compression=zipfile.ZIP_DEFLATED) as snapshot:
                total = 0
                if len(files) > 512: raise ValueError('Too many key files')
                for file in files:
                    if file.is_symlink() or not file.name.endswith('.xml'):
                        raise ValueError('Only regular keyring XML files are accepted')
                    input_fd = os.open(file, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK)
                    with os.fdopen(input_fd, 'rb') as input_file:
                        if not stat.S_ISREG(os.fstat(input_file.fileno()).st_mode):
                            raise ValueError('Not a regular key file')
                        data = input_file.read(16 * 1024 * 1024 + 1)
                    total += len(data)
                    if total > 16 * 1024 * 1024: raise ValueError('Keyring too large')
                    snapshot.writestr(file.name, data)
            output.flush()
            os.fsync(output.fileno())
        with zipfile.ZipFile(temporary) as verified:
            if verified.testzip() is not None: raise ValueError('Snapshot integrity failed')
        # Atomic no-clobber publication on the same filesystem.
        os.link(temporary, archive)
    finally:
        Path(temporary).unlink(missing_ok=True)


def restore(archive, target):
    import shutil
    import stat
    target = Path(target)
    if target.exists() or target.is_symlink():
        raise ValueError('Restore target must not exist')
    with zipfile.ZipFile(archive) as snapshot:
        members = snapshot.infolist()
        names = [member.filename for member in members]
        if not members or len(members) > 512 or len(set(names)) != len(names):
            raise ValueError('Invalid snapshot entries')
        if sum(member.file_size for member in members) > 16 * 1024 * 1024:
            raise ValueError('Snapshot too large')
        for member in members:
            name = member.filename
            if (Path(name).name != name or '/' in name or '\\' in name
                    or not name.endswith('.xml') or name in {'.', '..'}
                    or stat.S_ISLNK(member.external_attr >> 16) or member.is_dir()):
                raise ValueError('Unsafe snapshot entry')
        # Validate every CRC before creating the restored directory.
        contents = [(member.filename, snapshot.read(member)) for member in members]
    temporary = Path(tempfile.mkdtemp(prefix='.keyring-restore-', dir=target.parent))
    try:
        for name, data in contents:
            destination = temporary / name
            fd = os.open(destination, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
            with os.fdopen(fd, 'wb') as output:
                output.write(data); output.flush(); os.fsync(output.fileno())
        os.rename(temporary, target)
    finally:
        if temporary.exists(): shutil.rmtree(temporary)



def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('operation', choices=['backup', 'restore'])
    parser.add_argument('source'); parser.add_argument('destination')
    parser.add_argument('--quiesced', action='store_true')
    args = parser.parse_args()
    try:
        if args.operation == 'backup': backup(args.source, args.destination, args.quiesced)
        else: restore(args.source, args.destination)
    except (OSError, ValueError, zipfile.BadZipFile):
        parser.exit(1, 'Keyring snapshot failed; check inputs/permissions. No key contents logged.\n')
    print('Local keyring operation completed; no encryption, upload or deployment performed.')

if __name__ == '__main__': main()
