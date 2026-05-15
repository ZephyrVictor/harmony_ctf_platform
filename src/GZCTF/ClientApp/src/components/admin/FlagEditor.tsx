import {
  ActionIcon,
  Alert,
  Badge,
  Button,
  Group,
  Modal,
  NumberInput,
  Select,
  SimpleGrid,
  Stack,
  Switch,
  Table,
  Text,
  TextInput,
  Title,
} from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import { mdiAlertCircleOutline, mdiDelete, mdiPencil, mdiPlus } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  ChallengeFlag,
  ChallengeFlagAnswerType,
  ChallengeFlagInput,
  getFlags,
  createFlag,
  updateFlag,
  deleteFlag,
} from '@Utils/ExtApi'
import { getInputNumber, showErrorMsg, showSuccessMsg } from '@Utils/Shared'
import { FlagMatchMode } from '@Api'

interface FlagEditorProps {
  challengeId: number
  flagMatchMode?: FlagMatchMode | null
  disabled?: boolean
  disabledReason?: string
}

interface FlagFormValues {
  flagValue: string
  answerType: ChallengeFlagAnswerType
  rawAnswer: string
  isVisible: boolean
  isEnabled: boolean
  flawNo: string
  repository: string
  module: string
  filePath: string
  cwe: string
  score: number
  sortOrder: number
}

const createEmptyFlagForm = (sortOrder = 0): FlagFormValues => ({
  flagValue: '',
  answerType: ChallengeFlagAnswerType.SingleLine,
  rawAnswer: '',
  isVisible: true,
  isEnabled: true,
  flawNo: '',
  repository: '',
  module: '',
  filePath: '',
  cwe: '',
  score: 100,
  sortOrder,
})

const cleanText = (value: string) => value.trim()

export const FlagEditor: FC<FlagEditorProps> = ({ challengeId, flagMatchMode, disabled, disabledReason }) => {
  const { t } = useTranslation()
  const isHybridLine = flagMatchMode === FlagMatchMode.HybridLine
  const isLocked = disabled === true
  const [flags, setFlags] = useState<ChallengeFlag[]>([])
  const [loading, setLoading] = useState(true)
  const [editModal, editModalHandlers] = useDisclosure(false)
  const [editingFlag, setEditingFlag] = useState<ChallengeFlag | null>(null)
  const [formValues, setFormValues] = useState<FlagFormValues>(createEmptyFlagForm())
  const [saving, setSaving] = useState(false)

  const loadFlags = useCallback(async () => {
    try {
      const data = await getFlags(challengeId)
      setFlags(data)
    } catch (err: any) {
      showErrorMsg(err, t)
    } finally {
      setLoading(false)
    }
  }, [challengeId])

  useEffect(() => {
    loadFlags()
  }, [loadFlags])

  const totalScore = flags.reduce((sum, f) => sum + f.score, 0)

  const openCreate = () => {
    if (isLocked) return
    setEditingFlag(null)
    setFormValues(createEmptyFlagForm(flags.length))
    editModalHandlers.open()
  }

  const openEdit = (flag: ChallengeFlag) => {
    if (isLocked) return
    setEditingFlag(flag)
    setFormValues({
      flagValue: flag.flagValue,
      answerType: flag.answerType ?? ChallengeFlagAnswerType.SingleLine,
      rawAnswer: flag.rawAnswer ?? flag.flagValue,
      isVisible: flag.isVisible ?? true,
      isEnabled: flag.isEnabled ?? true,
      flawNo: flag.flawNo ?? '',
      repository: flag.repository ?? '',
      module: flag.module ?? '',
      filePath: flag.filePath ?? '',
      cwe: flag.cwe ?? '',
      score: flag.score,
      sortOrder: flag.sortOrder,
    })
    editModalHandlers.open()
  }

  const answerTypeData = [
    {
      value: ChallengeFlagAnswerType.SingleLine,
      label: t('admin.flag.answer_type.single_line'),
    },
    {
      value: ChallengeFlagAnswerType.LineRange,
      label: t('admin.flag.answer_type.line_range'),
    },
    {
      value: ChallengeFlagAnswerType.MultiLine,
      label: t('admin.flag.answer_type.multi_line'),
    },
    {
      value: ChallengeFlagAnswerType.Exact,
      label: t('admin.flag.answer_type.exact'),
    },
  ]

  const answerTypeLabel = (type?: ChallengeFlagAnswerType) =>
    answerTypeData.find((item) => item.value === type)?.label ?? t('admin.flag.answer_type.single_line')

  const buildPayload = (): ChallengeFlagInput & { flagValue: string; score: number; sortOrder: number } => {
    const base = {
      flagValue: formValues.flagValue.trim(),
      score: formValues.score,
      sortOrder: formValues.sortOrder,
    }

    if (!isHybridLine) {
      return base
    }

    const answer = formValues.rawAnswer.trim() || base.flagValue

    return {
      ...base,
      answerType: formValues.answerType,
      rawAnswer: answer,
      isVisible: formValues.isVisible,
      isEnabled: formValues.isEnabled,
      flawNo: cleanText(formValues.flawNo),
      repository: cleanText(formValues.repository),
      module: cleanText(formValues.module),
      filePath: cleanText(formValues.filePath),
      cwe: cleanText(formValues.cwe),
    }
  }

  const handleSave = async () => {
    if (isLocked) return
    if (!formValues.flagValue.trim()) {
      showErrorMsg(t('admin.flag.empty_value'), t)
      return
    }
    if (formValues.score < 1) {
      showErrorMsg(t('admin.flag.invalid_score'), t)
      return
    }

    setSaving(true)
    try {
      const payload = buildPayload()
      if (editingFlag) {
        await updateFlag(challengeId, editingFlag.id, payload)
      } else {
        await createFlag(challengeId, payload)
      }
      showSuccessMsg(t('common.content.save_success'), t)
      editModalHandlers.close()
      await loadFlags()
    } catch (err: any) {
      showErrorMsg(err, t)
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (flag: ChallengeFlag) => {
    if (isLocked) return
    if (!confirm(t('admin.flag.confirm_delete'))) return
    try {
      await deleteFlag(challengeId, flag.id)
      showSuccessMsg(t('common.content.delete_success'), t)
      await loadFlags()
    } catch (err: any) {
      showErrorMsg(err, t)
    }
  }

  return (
    <Stack gap="md">
      <Group justify="space-between">
        <Title order={5}>
          {t('admin.flag.title')}
          <Badge ml="sm" variant="light">
            {t('admin.flag.total_score')}: {totalScore}
          </Badge>
        </Title>
        <Button leftSection={<Icon path={mdiPlus} size={1} />} size="xs" onClick={openCreate} disabled={isLocked}>
          {t('admin.flag.add')}
        </Button>
      </Group>

      {isLocked && disabledReason && (
        <Alert color="yellow" icon={<Icon path={mdiAlertCircleOutline} size={1} />}>
          {disabledReason}
        </Alert>
      )}

      {flags.length === 0 && !loading ? (
        <Text c="dimmed" size="sm">
          {t('admin.flag.no_flags')}
        </Text>
      ) : (
        <Table>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>{t('admin.flag.column.value')}</Table.Th>
              {isHybridLine && <Table.Th>{t('admin.flag.column.answer_type')}</Table.Th>}
              {isHybridLine && <Table.Th>{t('admin.flag.column.metadata')}</Table.Th>}
              {isHybridLine && <Table.Th>{t('admin.flag.column.status')}</Table.Th>}
              <Table.Th>{t('admin.flag.column.score')}</Table.Th>
              <Table.Th>{t('admin.flag.column.sort')}</Table.Th>
              <Table.Th>{t('admin.flag.column.actions')}</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {flags.map((flag) => (
              <Table.Tr key={flag.id}>
                <Table.Td>
                  <Text size="sm" ff="monospace">
                    {flag.flagValue}
                  </Text>
                </Table.Td>
                {isHybridLine && (
                  <Table.Td>
                    <Badge variant="light">{answerTypeLabel(flag.answerType)}</Badge>
                  </Table.Td>
                )}
                {isHybridLine && (
                  <Table.Td>
                    <Stack gap={2}>
                      {flag.flawNo && <Text size="xs">{flag.flawNo}</Text>}
                      {flag.repository && (
                        <Text size="xs" c="dimmed">
                          {flag.repository}
                        </Text>
                      )}
                      {flag.module && (
                        <Text size="xs" c="dimmed">
                          {flag.module}
                        </Text>
                      )}
                      {flag.filePath && (
                        <Text size="xs" ff="monospace" lineClamp={1}>
                          {flag.filePath}
                        </Text>
                      )}
                      {flag.cwe && (
                        <Text size="xs" c="dimmed">
                          {flag.cwe}
                        </Text>
                      )}
                    </Stack>
                  </Table.Td>
                )}
                {isHybridLine && (
                  <Table.Td>
                    <Group gap="xs">
                      <Badge variant="light" color={(flag.isVisible ?? true) ? 'blue' : 'gray'}>
                        {(flag.isVisible ?? true) ? t('admin.flag.visible') : t('admin.flag.hidden')}
                      </Badge>
                      <Badge variant="light" color={(flag.isEnabled ?? true) ? 'green' : 'gray'}>
                        {(flag.isEnabled ?? true) ? t('admin.flag.enabled') : t('admin.flag.disabled')}
                      </Badge>
                    </Group>
                  </Table.Td>
                )}
                <Table.Td>
                  <Badge variant="light" color="green">
                    +{flag.score}
                  </Badge>
                </Table.Td>
                <Table.Td>{flag.sortOrder}</Table.Td>
                <Table.Td>
                  <Group gap="xs">
                    <ActionIcon variant="subtle" size="sm" onClick={() => openEdit(flag)} disabled={isLocked}>
                      <Icon path={mdiPencil} size={0.8} />
                    </ActionIcon>
                    <ActionIcon variant="subtle" color="red" size="sm" onClick={() => handleDelete(flag)} disabled={isLocked}>
                      <Icon path={mdiDelete} size={0.8} />
                    </ActionIcon>
                  </Group>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      )}

      <Modal
        opened={editModal}
        onClose={editModalHandlers.close}
        title={editingFlag ? t('admin.flag.edit') : t('admin.flag.add')}
      >
        <Stack gap="md">
          <TextInput
            label={t('admin.flag.column.value')}
            value={formValues.flagValue}
            onChange={(e) =>
              setFormValues({ ...formValues, flagValue: e.currentTarget.value, rawAnswer: e.currentTarget.value })
            }
            required
          />
          {isHybridLine && (
            <>
              <SimpleGrid cols={{ base: 1, sm: 2 }}>
                <Select
                  label={t('admin.flag.column.answer_type')}
                  value={formValues.answerType}
                  data={answerTypeData}
                  onChange={(value) =>
                    setFormValues({
                      ...formValues,
                      answerType: (value as ChallengeFlagAnswerType) ?? ChallengeFlagAnswerType.SingleLine,
                    })
                  }
                  required
                />
                <Group align="flex-end" gap="lg">
                  <Switch
                    checked={formValues.isVisible}
                    label={t('admin.flag.visible')}
                    onChange={(e) => setFormValues({ ...formValues, isVisible: e.currentTarget.checked })}
                  />
                  <Switch
                    checked={formValues.isEnabled}
                    label={t('admin.flag.enabled')}
                    onChange={(e) => setFormValues({ ...formValues, isEnabled: e.currentTarget.checked })}
                  />
                </Group>
              </SimpleGrid>
              <SimpleGrid cols={{ base: 1, sm: 2 }}>
                <TextInput
                  label={t('admin.flag.metadata.flaw_no')}
                  value={formValues.flawNo}
                  onChange={(e) => setFormValues({ ...formValues, flawNo: e.currentTarget.value })}
                />
                <TextInput
                  label={t('admin.flag.metadata.cwe')}
                  value={formValues.cwe}
                  onChange={(e) => setFormValues({ ...formValues, cwe: e.currentTarget.value })}
                />
                <TextInput
                  label={t('admin.flag.metadata.repository')}
                  value={formValues.repository}
                  onChange={(e) => setFormValues({ ...formValues, repository: e.currentTarget.value })}
                />
                <TextInput
                  label={t('admin.flag.metadata.module')}
                  value={formValues.module}
                  onChange={(e) => setFormValues({ ...formValues, module: e.currentTarget.value })}
                />
              </SimpleGrid>
              <TextInput
                label={t('admin.flag.metadata.file_path')}
                value={formValues.filePath}
                onChange={(e) => setFormValues({ ...formValues, filePath: e.currentTarget.value })}
              />
            </>
          )}
          <NumberInput
            label={t('admin.flag.column.score')}
            value={formValues.score}
            onChange={(v) => {
              const num = getInputNumber(v)
              if (isNaN(num)) return
              setFormValues({ ...formValues, score: num })
            }}
            min={1}
            required
          />
          <NumberInput
            label={t('admin.flag.column.sort')}
            value={formValues.sortOrder}
            onChange={(v) => {
              const num = getInputNumber(v)
              if (isNaN(num)) return
              setFormValues({ ...formValues, sortOrder: num })
            }}
          />
          <Group justify="flex-end">
            <Button variant="default" onClick={editModalHandlers.close}>
              {t('common.content.cancel')}
            </Button>
            <Button onClick={handleSave} loading={saving} disabled={isLocked}>
              {t('common.content.save')}
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  )
}
