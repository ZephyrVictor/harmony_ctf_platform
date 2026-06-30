import {
  Alert,
  Badge,
  Button,
  Card,
  FileButton,
  Group,
  MultiSelect,
  ScrollArea,
  Select,
  Stack,
  Text,
  Title,
  NumberInput,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiFileUploadOutline, mdiDatabaseImportOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useCallback, useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { showErrorMsg } from '@Utils/Shared'
import api, {
  ChallengeFlagAnswerType,
  FlagImportFilePreview,
  FlagImportResult,
} from '@Api'
import {
  FlagImportColumnMapping,
  ImportMode,
  LevelColumnMapping,
} from '@Utils/ManualApiTypes'

const AdminImportFlags: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { t } = useTranslation()
  const [file, setFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<FlagImportFilePreview | null>(null)
  const [result, setResult] = useState<FlagImportResult | null>(null)
  const [disabled, setDisabled] = useState(false)

  const [matchBy, setMatchBy] = useState<string>('FilePath')
  const [repoCol, setRepoCol] = useState<string | null>(null)
  const [submodCol, setSubmodCol] = useState<string | null>(null)
  const [trackTitleCol, setTrackTitleCol] = useState<string | null>(null)
  const [trackFlawNoCol, setTrackFlawNoCol] = useState<string | null>(null)
  const [trackFilePathCol, setTrackFilePathCol] = useState<string | null>(null)
  const [trackCategoryCol, setTrackCategoryCol] = useState<string | null>(null)
  const [trackRefUrlCol, setTrackRefUrlCol] = useState<string | null>(null)
  const [trackModuleCol, setTrackModuleCol] = useState<string | null>(null)

  const [levelCols, setLevelCols] = useState<LevelColumnMapping[]>([
    { level: 1, columnName: '', answerType: ChallengeFlagAnswerType.Exact, score: 5 },
    { level: 2, columnName: '', answerType: ChallengeFlagAnswerType.Exact, score: 4 },
    { level: 3, columnName: '', answerType: ChallengeFlagAnswerType.Exact, score: 1 },
  ])

  const doPreview = useCallback(
    async (f: File, m: FlagImportColumnMapping) => {
      setDisabled(true)
      try {
        const res = await api.flagImport.flagImportPreviewGame(numId, {
          file: f,
          mapping: JSON.stringify(m),
        })
        setPreview(res.data)
        return res.data
      } catch (e) {
        showErrorMsg(e, t)
        return null
      } finally {
        setDisabled(false)
      }
    },
    [numId, t]
  )

  const autoDetect = useCallback(
    (data: FlagImportFilePreview) => {
      if (!data.columnNames) return
      const cols = data.columnNames
      const find = (pred: (s: string) => boolean) => cols.find(pred)
      setRepoCol((v) => v ?? find((c) => c.includes('仓库') || c.toLowerCase().includes('repo')) ?? null)
      setSubmodCol((v) => v ?? find((c) => c.includes('子模块') || c.toLowerCase().includes('submod')) ?? null)
      setTrackTitleCol((v) => v ?? find((c) => c.includes('title')) ?? null)
      setTrackFlawNoCol((v) => v ?? find((c) => c.toLowerCase().includes('flaw')) ?? null)
      setTrackFilePathCol(
        (v) => v ?? find((c) => c.includes('文件') || c.toLowerCase().includes('file')) ?? null
      )
      setTrackCategoryCol(
        (v) => v ?? find((c) => c.includes('类别') || c.toLowerCase().includes('categ')) ?? null
      )
      setTrackRefUrlCol(
        (v) => v ?? find((c) => c.includes('PR') || c.includes('参考') || c.toLowerCase().includes('ref')) ?? null
      )
      setTrackModuleCol((v) => v ?? find((c) => c.includes('模块') || c.toLowerCase().includes('module')) ?? null)
      setLevelCols((ls) =>
        ls.map((l, i) => {
          if (l.columnName) return l
          const col =
            i === 0
              ? find((c) => c.includes('文件') || c.toLowerCase().includes('file'))
              : i === 1
                ? find((c) => c.includes('commit'))
                : find((c) => c.includes('修复研判') || c.toLowerCase().includes('revised')) ??
                  find((c) => c.toLowerCase().includes('cwe'))
          // Auto-detect extra columns for Level 1 (e.g. line number)
          const extraCols =
            i === 0
              ? cols.filter((c) => c.includes('行数') || c.toLowerCase().includes('line'))
              : []
          return { ...l, columnName: col ?? '', extraColumns: extraCols.length > 0 ? extraCols : l.extraColumns }
        })
      )
    },
    []
  )

  const initialDetectDone = useRef(false)

  const onFileSelected = async (f: File | null) => {
    if (!f) return
    setFile(f)
    setPreview(null)
    setResult(null)
    initialDetectDone.current = false
    const res = await doPreview(f, buildMapping())
    if (res) {
      autoDetect(res)
      initialDetectDone.current = true
    }
  }

  // Re-preview when mapping fields change (after initial auto-detection)
  useEffect(() => {
    if (!file || !initialDetectDone.current) return
    const timer = setTimeout(() => {
      doPreview(file, buildMapping())
    }, 300)
    return () => clearTimeout(timer)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [repoCol, submodCol, trackTitleCol, trackFlawNoCol, trackFilePathCol, trackCategoryCol, trackRefUrlCol, trackModuleCol, matchBy, levelCols])

  const buildMapping = (): FlagImportColumnMapping => ({
    trackTitleColumn: trackTitleCol ?? undefined,
    trackFlawNoColumn: trackFlawNoCol ?? undefined,
    trackRepositoryColumn: repoCol ?? undefined,
    trackModuleColumn: trackModuleCol ?? undefined,
    trackFilePathColumn: trackFilePathCol ?? undefined,
    trackCategoryColumn: trackCategoryCol ?? undefined,
    trackReferenceUrlColumn: trackRefUrlCol ?? undefined,
    repositoryColumn: repoCol ?? undefined,
    submoduleColumn: submodCol ?? undefined,
    matchBy,
    levelColumns: levelCols,
  })

  const onImport = async () => {
    if (!file) return
    if (!repoCol || !submodCol) {
      showNotification({
        color: 'red',
        message: t('admin.notification.games.import.repo_submod_required', {
          defaultValue: 'Repository and submodule columns are required for game-level import',
        }),
      })
      return
    }
    setDisabled(true)
    try {
      const res = await api.flagImport.flagImportImportGame(numId, {
        file,
        mapping: JSON.stringify(buildMapping()),
        mode: ImportMode.Upsert,
      })
      setResult(res.data)
      showNotification({
        color: 'teal',
        message: `Imported: ${res.data.imported} / Created challenges: ${res.data.createdChallenges} / Reused: ${res.data.reusedChallenges} / Failed: ${res.data.failed}`,
        icon: <Icon path={mdiCheck} size={1} />,
      })
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  const columnOptions = (preview?.columnNames ?? []).map((c) => ({ value: c, label: c }))

  return (
    <WithGameEditTab>
      <Stack p="md">
        <Group justify="space-between">
          <Title>Import Flags (Game-level: auto-create challenges)</Title>
          <FileButton onChange={onFileSelected} accept=".xlsx,.csv">
            {(props) => (
              <Button {...props} leftSection={<Icon path={mdiFileUploadOutline} size={1} />}>
                {t('admin.button.challenges.import.select_file', { defaultValue: 'Select File' })}
              </Button>
            )}
          </FileButton>
        </Group>
      <Alert icon={<Icon path={mdiDatabaseImportOutline} size={1} />} color="blue" variant="light">
        {t('admin.content.games.import.help', {
          defaultValue:
            'Upload an xlsx/csv. Each unique (Repository, Submodule) combination creates a GameChallenge automatically. Each row becomes a Track with multi-level flags.',
        })}
      </Alert>
      {file && (
        <Group>
          <Badge variant="light">File: {file.name}</Badge>
          <Badge variant="light">{(file.size / 1024).toFixed(1)} KB</Badge>
        </Group>
      )}
      {preview && (
        <>
          <Card withBorder padding="md">
            <Stack gap="sm">
              <Title order={5}>Preview</Title>
              <Group>
                <Badge variant="light" color="blue">
                  Repositories: {preview.repositories?.length ?? 0}
                </Badge>
                <Badge variant="light" color="teal">
                  Submodules: {preview.submodules?.length ?? 0}
                </Badge>
                <Badge variant="light" color="indigo">
                  Tracks: {preview.tracks?.length ?? 0}
                </Badge>
              </Group>
              {preview.repositories && preview.repositories.length > 0 && (
                <Text size="sm">
                  <strong>Repositories:</strong> {preview.repositories.join(', ')}
                </Text>
              )}
              {preview.submodules && preview.submodules.length > 0 && (
                <Text size="sm">
                  <strong>Submodules:</strong> {preview.submodules.join(', ')}
                </Text>
              )}
            </Stack>
          </Card>
          <Card withBorder padding="md">
            <Stack gap="sm">
              <Title order={5}>Column Mapping</Title>
              <Group grow>
                <Select label="Repository column (required)" value={repoCol} onChange={setRepoCol} data={columnOptions} />
                <Select label="Submodule column (required)" value={submodCol} onChange={setSubmodCol} data={columnOptions} />
                <Select label="MatchBy" value={matchBy} onChange={(v) => setMatchBy(v ?? 'FilePath')} data={['FilePath', 'FlawNo', 'Title']} />
              </Group>
              <Group grow>
                <Select label="Title column" value={trackTitleCol} onChange={setTrackTitleCol} data={columnOptions} clearable />
                <Select label="FlawNo column" value={trackFlawNoCol} onChange={setTrackFlawNoCol} data={columnOptions} clearable />
                <Select label="FilePath column" value={trackFilePathCol} onChange={setTrackFilePathCol} data={columnOptions} clearable />
              </Group>
              <Group grow>
                <Select label="Module column" value={trackModuleCol} onChange={setTrackModuleCol} data={columnOptions} clearable />
                <Select label="Category column" value={trackCategoryCol} onChange={setTrackCategoryCol} data={columnOptions} clearable />
                <Select label="ReferenceUrl column" value={trackRefUrlCol} onChange={setTrackRefUrlCol} data={columnOptions} clearable />
              </Group>
              <Title order={6} mt="sm">
                Level columns
              </Title>
              <Text size="xs" c="dimmed">
                Extra columns are concatenated to the main column with ":" separator. E.g. Column="文件路径" + Extra=["代码行数"] → "path.cpp:c:856-864"
              </Text>
              {levelCols.map((lc, idx) => (
                <Group key={idx} grow align="flex-end">
                  <NumberInput
                    label={`Level ${idx + 1}`}
                    value={lc.level}
                    onChange={(v) =>
                      setLevelCols((ls) => ls.map((l, i) => (i === idx ? { ...l, level: typeof v === 'number' ? v : 1 } : l)))
                    }
                  />
                  <Select
                    label="Column"
                    value={lc.columnName}
                    onChange={(v) => setLevelCols((ls) => ls.map((l, i) => (i === idx ? { ...l, columnName: v ?? '' } : l)))}
                    data={columnOptions}
                  />
                  <MultiSelect
                    label="Extra columns"
                    placeholder="Concat with ':'"
                    value={lc.extraColumns ?? []}
                    onChange={(v) => setLevelCols((ls) => ls.map((l, i) => (i === idx ? { ...l, extraColumns: v } : l)))}
                    data={columnOptions}
                    clearable
                  />
                  <Select
                    label="AnswerType"
                    value={lc.answerType}
                    onChange={(v) =>
                      setLevelCols((ls) =>
                        ls.map((l, i) => (i === idx ? { ...l, answerType: v as ChallengeFlagAnswerType } : l))
                      )
                    }
                    data={Object.values(ChallengeFlagAnswerType).map((v) => ({ value: v, label: v }))}
                  />
                  <NumberInput
                    label="Score"
                    value={lc.score ?? 0}
                    onChange={(v) =>
                      setLevelCols((ls) => ls.map((l, i) => (i === idx ? { ...l, score: typeof v === 'number' ? v : 0 } : l)))
                    }
                  />
                </Group>
              ))}
              <Group justify="flex-end">
                <Button disabled={disabled} onClick={onImport} leftSection={<Icon path={mdiDatabaseImportOutline} size={1} />}>
                  {t('admin.button.challenges.import.run', { defaultValue: 'Import' })}
                </Button>
              </Group>
            </Stack>
          </Card>
        </>
      )}
      {preview?.tracks && preview.tracks.length > 0 && (
        <Card withBorder padding="md">
          <Title order={5} mb="sm">
            Track Preview ({preview.tracks.length} tracks)
          </Title>
          <ScrollArea h={300}>
            <Stack gap="xs">
              {preview.tracks.slice(0, 30).map((tr, i) => (
                <Group key={i} gap="xs">
                  <Badge variant="outline">#{i + 1}</Badge>
                  {tr.repository && (
                    <Badge variant="light" color="blue">
                      {tr.repository}
                    </Badge>
                  )}
                  {tr.submodule && (
                    <Badge variant="light" color="teal">
                      {tr.submodule}
                    </Badge>
                  )}
                  <Text size="sm" ff="monospace">
                    {tr.filePath ?? tr.title ?? '(no title)'}
                  </Text>
                  {tr.levels?.map((l, j) => (
                    <Badge key={j} variant="light" color="indigo">
                      L{l.level}: {l.flagValue}
                    </Badge>
                  ))}
                </Group>
              ))}
              {preview.tracks.length > 30 && (
                <Text size="xs" c="dimmed">
                  ... and {preview.tracks.length - 30} more
                </Text>
              )}
            </Stack>
          </ScrollArea>
        </Card>
      )}
      {result && (
        <Card withBorder padding="md">
          <Title order={5} mb="sm">
            Import Result
          </Title>
          <Group gap="md">
            <Badge variant="light" color="blue">
              Total: {result.totalRows}
            </Badge>
            <Badge variant="light" color="teal">
              Imported: {result.imported}
            </Badge>
            <Badge variant="light" color="yellow">
              Updated: {result.updated}
            </Badge>
            <Badge variant="light" color="green">
              Created Challenges: {result.createdChallenges}
            </Badge>
            <Badge variant="light" color="cyan">
              Reused Challenges: {result.reusedChallenges}
            </Badge>
            <Badge variant="light" color="red">
              Failed: {result.failed}
            </Badge>
          </Group>
          {result.errors && result.errors.length > 0 && (
            <Stack gap="xs" mt="sm">
              {result.errors.slice(0, 20).map((err, i) => (
                <Text key={i} size="xs" c="red">
                  Row {err.row}: {err.error}
                </Text>
              ))}
            </Stack>
          )}
        </Card>
      )}
      </Stack>
    </WithGameEditTab>
  )
}

export default AdminImportFlags
