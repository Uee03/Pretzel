# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [0.2.0] - 2026-10-27

First published release. Version 0.1.0 was internal and never tagged.

### Added

- `DataIO` and `DataStore`: keys, slots, recoverable writes, schema migrations, pluggable serializers and plain, binary and AES formats, plus the Data Format window.
- `SaveSlots<T>`: numbered save slots with `SaveSlotInfo` metadata, newest-first listing, free-slot search and PNG thumbnails.
- `AutoSaveTrigger`: requests saves on an interval and on pause, focus loss and quit.
- EditMode tests for save slots.
