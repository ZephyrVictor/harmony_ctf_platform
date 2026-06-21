import {
  ActionIcon,
  Alert,
  Badge,
  Button,
  Group,
  Modal,
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
import api, {
  ChallengeFlag,
  ChallengeFlagAnswerType,
  ChallengeFlagCreateModel,
  ChallengeFlagUpdateModel,
} from '@Api'
import { showErrorMsg, showSuccessMsg } from '@Utils/Shared'

interface FlagEditorProps {
  challengeId: number
  disabled?: boolean
  disabledReason?: string
}

interface FlagFormValues {
  flagValue: string
  answerType: ChallengeFlagAnswerType
  rawAnswer: string
  isVisible: boolean
  isEnabled: boolean
  level: number
  trackId: string
  sortOrder: number
}

const createEmptyFlagForm = (sortOrder = 0): FlagFormValues => ({
  flagValue: '',
  answerType: ChallengeFlagAnswerType.SingleLine,
  rawAnswer: '',
  isVisible: true,
  isEnabled: true,
  level: 1,
  trackId: '',
  sortOrder,
})

export const FlagEditor: FC<FlagEditorProps> = ({ challengeId, disabled, disabledReason }) => {
  const { t } = useTranslation()
  const isLocked = disabled === true
  const [flags, setFlags] = useState<ChallengeFlag[]>([])
  const [loading, setLoading] = useState(true)
  const [editModal, editModalHandlers] = useDisclosure(false)
  const [editingFlag, setEditingFlag] = useState<ChallengeFlag | null>(null)
  const [formValues, setFormValues] = useState<FlagFormValues>(createEmptyFlagForm())
  const [saving, setSaving] = useState(false)

  const loadFlags = useCallback(async () => {
    try {
      const res = await api.ext.extGetFlags(challengeId)
      setFlags(res.data)
    } catch (err: any) {
      showErrorMsg(err, t)
    } finally {
      setLoading(false)
    }
  }, [challengeId])

  useEffect(() => {
    loadFlags()
  }, [loadFlags])

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
      level: flag.level ?? 1,
      trackId: flag.trackId ?? '',
      sortOrder: flag.sortOrder ?? 0,
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

  const buildPayload = (): ChallengeFlagCreateModel => {
    const answer = formValues.rawAnswer.trim() || formValues.flagValue.trim()
    const payload: ChallengeFlagCreateModel = {
      flagValue: formValues.flagValue.trim(),
      answerType: formValues.answerType,
      rawAnswer: answer,
      isVisible: formValues.isVisible,
      isEnabled: formValues.isEnabled,
      level: formValues.level,
      sortOrder: formValues.sortOrder,
    }
    if (formValues.trackId.trim()) {
      payload.trackId = formValues.trackId.trim()
    }
    return payload
  }

  const handleSave = async () => {
    if (isLocked) return
    if (!formValues.flagValue.trim()) {
      showErrorMsg(t('admin.flag.empty_value'), t)
      return
    }
    if (formValues.level < 1) {
      showErrorMsg(t('admin.flag.invalid_level'), t)
      return
    }

    setSaving(true)
    try {
      const payload = buildPayload()
      if (editingFlag) {
        const updatePayload: ChallengeFlagUpdateModel = {
          flagValue: payload.flagValue,
          answerType: payload.answerType,
          rawAnswer: payload.rawAnswer,
          isVisible: payload.isVisible,
          isEnabled: payload.isEnabled,
          level: payload.level,
          trackId: payload.trackId,
          sortOrder: payload.sortOrder,
        }
        await api.ext.extUpdateFlag(challengeId, editingFlag.id ?? '', updatePayload)
      } else {
        await api.ext.extCreateFlag(challengeId, payload)
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
      await api.ext.extDeleteFlag(challengeId, flag.id ?? '')
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
            {t('admin.flag.total_count')}: {flags.length}
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
              <Table.Th>{t('admin.flag.column.answer_type')}</Table.Th>
              <Table.Th>{t('admin.flag.column.level')}</Table.Th>
              <Table.Th>{t('admin.flag.column.track')}</Table.Th>
              <Table.Th>{t('admin.flag.column.status')}</Table.Th>
              <Table.Th>{t('admin.flag.column.sort')}</Table.Th>
              <Table.Th>{t('admin.flag.column.actions')}</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {flags.map((flag) => (
              <Table.Tr key={flag.id ?? ''}>
                <Table.Td>
                  <Text size="sm" ff="monospace">
                    {flag.flagValue}
                  </Text>
                </Table.Td>
                <Table.Td>
                  <Badge variant="light">{answerTypeLabel(flag.answerType)}</Badge>
                </Table.Td>
                <Table.Td>
                  <Badge variant="light" color="indigo">
                    L{flag.level ?? 1}
                  </Badge>
                </Table.Td>
                <Table.Td>
                  <Text size="xs" ff="monospace" lineClamp={1}>
                    {flag.trackId ? flag.trackId.slice(0, 8) : '-'}
                  </Text>
                </Table.Td>
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
                <Table.Td>{flag.sortOrder ?? 0}</Table.Td>
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
              label={t('admin.flag.column.level')}
              type="number"
              value={formValues.level}
              onChange={(e) => {
                const num = parseInt(e.currentTarget.value, 10)
                if (!isNaN(num)) setFormValues({ ...formValues, level: num })
              }}
              min={1}
              required
            />
            <TextInput
              label={t('admin.flag.column.track')}
              value={formValues.trackId}
              onChange={(e) => setFormValues({ ...formValues, trackId: e.currentTarget.value })}
              placeholder={t('admin.flag.column.track_optional')}
            />
          </SimpleGrid>
          <TextInput
            label={t('admin.flag.column.sort')}
            type="number"
            value={formValues.sortOrder}
            onChange={(e) => {
              const num = parseInt(e.currentTarget.value, 10)
              if (!isNaN(num)) setFormValues({ ...formValues, sortOrder: num })
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
