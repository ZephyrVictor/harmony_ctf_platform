import {
  Alert,
  Badge,
  Button,
  Card,
  FileButton,
  Group,
  ScrollArea,
  Select,
  Stack,
  Text,
  TextInput,
  Title,
  NumberInput,
  Switch,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiFileUploadOutline, mdiDatabaseImportOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithChallengeEdit } from '@Components/admin/WithChallengeEdit'
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

const AdminImport: FC = () => {
  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]
  const { t } = useTranslation()
  const [file, setFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<FlagImportFilePreview | null>(null)
  const [result, setResult] = useState<FlagImportResult | null>(null)
  const [disabled, setDisabled] = useState(false)

  const [matchBy, setMatchBy] = useState<string>('FilePath')
  const [trackTitleCol, setTrackTitleCol] = useState<string | null>(null)
  const [trackFlawNoCol, setTrackFlawNoCol] = useState<string | null>(null)
  const [trackRepoCol, setTrackRepoCol] = useState<string | null>(null)
  const [trackModuleCol, setTrackModuleCol] = useState<string | null>(null)
  const [trackFilePathCol, setTrackFilePathCol] = useState<string | null>(null)
  const [trackCategoryCol, setTrackCategoryCol] = useState<string | null>(null)
  const [trackRefUrlCol, setTrackRefUrlCol] = useState<string | null>(null)

  const [levelCols, setLevelCols] = useState<LevelColumnMapping[]>([
    { level: 1, columnName: '', answerType: ChallengeFlagAnswerType.Exact, score: 5 },
    { level: 2, columnName: '', answerType: ChallengeFlagAnswerType.Exact, score: 4 },
    { level: 3, columnName: '', answerType: ChallengeFlagAnswerType.Exact, score: 1 },
  ])

  const onFileSelected = async (f: File | null) => {
    if (!f) return
    setFile(f)
    setPreview(null)
    setResult(null)
    setDisabled(true)
    try {
      const mapping = buildMapping()
      const res = await api.flagImport.flagImportPreviewChallenge(numCId, {
        file: f,
        mapping: JSON.stringify(mapping),
      })
      setPreview(res.data)
      // auto-fill column selects if empty
      if (res.data.columnNames) {
        const cols = res.data.columnNames
        const find = (pred: (s: string) => boolean) => cols.find(pred)
        setTrackTitleCol((v) => v ?? find((c) => c.includes('title')) ?? null)
        setTrackFlawNoCol((v) => v ?? find((c) => c.toLowerCase().includes('flaw')) ?? null)
        setTrackRepoCol((v) => v ?? find((c) => c.includes('仓库') || c.toLowerCase().includes('repo')) ?? null)
        setTrackModuleCol((v) => v ?? find((c) => c.includes('模块') || c.toLowerCase().includes('module')) ?? null)
        setTrackFilePathCol(
          (v) => v ?? find((c) => c.includes('文件') || c.toLowerCase().includes('file')) ?? null
        )
        setTrackCategoryCol((v) => v ?? find((c) => c.includes('类别') || c.toLowerCase().includes('categ')) ?? null)
        setTrackRefUrlCol(
          (v) => v ?? find((c) => c.includes('参考') || c.toLowerCase().includes('ref')) ?? null
        )
        setLevelCols((ls) =>
          ls.map((l, i) => {
            if (l.columnName) return l
            const col =
              i === 0
                ? find((c) => c.includes('文件') || c.toLowerCase().includes('file'))
                : i === 1
                  ? find((c) => c.includes('commit'))
                  : find((c) => c.toLowerCase().includes('cwe'))
            return { ...l, columnName: col ?? '' }
          })
        )
      }
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  const buildMapping = (): FlagImportColumnMapping => ({
    trackTitleColumn: trackTitleCol ?? undefined,
    trackFlawNoColumn: trackFlawNoCol ?? undefined,
    trackRepositoryColumn: trackRepoCol ?? undefined,
    trackModuleColumn: trackModuleCol ?? undefined,
    trackFilePathColumn: trackFilePathCol ?? undefined,
    trackCategoryColumn: trackCategoryCol ?? undefined,
    trackReferenceUrlColumn: trackRefUrlCol ?? undefined,
    matchBy,
    levelColumns: levelCols,
  })

  const onImport = async () => {
    if (!file) return
    setDisabled(true)
    try {
      const res = await api.flagImport.flagImportImportChallenge(numCId, {
        file,
        mapping: JSON.stringify(buildMapping()),
        mode: ImportMode.Upsert,
      })
      setResult(res.data)
      showNotification({
        color: 'teal',
        message: `Imported: ${res.data.imported} / Updated: ${res.data.updated} / Failed: ${res.data.failed}`,
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
    <WithChallengeEdit
      isLoading={false}
      backUrl={`/admin/games/${numId}/challenges/${numCId}`}
      head={
        <>
          <Title>Import Flags</Title>
          <FileButton onChange={onFileSelected} accept=".xlsx,.csv">
            {(props) => (
              <Button {...props} leftSection={<Icon path={mdiFileUploadOutline} size={1} />}>
                {t('admin.button.challenges.import.select_file', { defaultValue: 'Select File' })}
              </Button>
            )}
          </FileButton>
        </>
      }
    >
      <Stack>
        <Alert icon={<Icon path={mdiDatabaseImportOutline} size={1} />} color="blue" variant="light">
          {t('admin.content.games.challenges.import.help', {
            defaultValue:
              'Upload an xlsx/csv. Map columns to track metadata + level flag values. Each row becomes a Track with multi-level flags.',
          })}
        </Alert>
        {file && (
          <Group>
            <Badge variant="light">File: {file.name}</Badge>
            <Badge variant="light">{(file.size / 1024).toFixed(1)} KB</Badge>
          </Group>
        )}
        {preview && (
          <Card withBorder padding="md">
            <Stack gap="sm">
              <Title order={5}>Column Mapping</Title>
              <Group grow>
                <Select label="MatchBy" value={matchBy} onChange={(v) => setMatchBy(v ?? 'FilePath')} data={['FilePath', 'FlawNo', 'Title']} />
                <Select label="Title column" value={trackTitleCol} onChange={setTrackTitleCol} data={columnOptions} clearable />
                <Select label="FlawNo column" value={trackFlawNoCol} onChange={setTrackFlawNoCol} data={columnOptions} clearable />
              </Group>
              <Group grow>
                <Select label="Repository column" value={trackRepoCol} onChange={setTrackRepoCol} data={columnOptions} clearable />
                <Select label="Module column" value={trackModuleCol} onChange={setTrackModuleCol} data={columnOptions} clearable />
                <Select label="FilePath column" value={trackFilePathCol} onChange={setTrackFilePathCol} data={columnOptions} clearable />
              </Group>
              <Group grow>
                <Select label="Category column" value={trackCategoryCol} onChange={setTrackCategoryCol} data={columnOptions} clearable />
                <Select label="ReferenceUrl column" value={trackRefUrlCol} onChange={setTrackRefUrlCol} data={columnOptions} clearable />
              </Group>
              <Title order={6} mt="sm">
                Level columns
              </Title>
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
        )}
        {preview?.tracks && preview.tracks.length > 0 && (
          <Card withBorder padding="md">
            <Title order={5} mb="sm">
              Preview ({preview.tracks.length} tracks)
            </Title>
            <ScrollArea h={300}>
              <Stack gap="xs">
                {preview.tracks.slice(0, 20).map((tr, i) => (
                  <Group key={i} gap="xs">
                    <Badge variant="outline">#{i + 1}</Badge>
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
                {preview.tracks.length > 20 && (
                  <Text size="xs" c="dimmed">
                    ... and {preview.tracks.length - 20} more
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
              <Badge variant="light" color="gray">
                Skipped: {result.skipped}
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
    </WithChallengeEdit>
  )
}

export default AdminImport
