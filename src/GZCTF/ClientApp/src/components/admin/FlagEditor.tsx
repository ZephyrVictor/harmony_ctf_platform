import {
  ActionIcon,
  Badge,
  Button,
  Group,
  Modal,
  NumberInput,
  Stack,
  Table,
  Text,
  TextInput,
  Title,
} from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import { mdiDelete, mdiPencil, mdiPlus } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { showErrorMsg, showSuccessMsg } from '@Utils/Shared'
import { ChallengeFlag, getFlags, createFlag, updateFlag, deleteFlag } from '@Utils/ExtApi'

interface FlagEditorProps {
  challengeId: number
}

export const FlagEditor: FC<FlagEditorProps> = ({ challengeId }) => {
  const { t } = useTranslation()
  const [flags, setFlags] = useState<ChallengeFlag[]>([])
  const [loading, setLoading] = useState(true)
  const [editModal, editModalHandlers] = useDisclosure(false)
  const [editingFlag, setEditingFlag] = useState<ChallengeFlag | null>(null)
  const [formValues, setFormValues] = useState({ flagValue: '', score: 100, sortOrder: 0 })
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
    setEditingFlag(null)
    setFormValues({ flagValue: '', score: 100, sortOrder: flags.length })
    editModalHandlers.open()
  }

  const openEdit = (flag: ChallengeFlag) => {
    setEditingFlag(flag)
    setFormValues({ flagValue: flag.flagValue, score: flag.score, sortOrder: flag.sortOrder })
    editModalHandlers.open()
  }

  const handleSave = async () => {
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
      if (editingFlag) {
        await updateFlag(challengeId, editingFlag.id, formValues)
      } else {
        await createFlag(challengeId, formValues)
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
        <Button
          leftSection={<Icon path={mdiPlus} size={1} />}
          size="xs"
          onClick={openCreate}
        >
          {t('admin.flag.add')}
        </Button>
      </Group>

      {flags.length === 0 && !loading ? (
        <Text c="dimmed" size="sm">{t('admin.flag.no_flags')}</Text>
      ) : (
        <Table>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>{t('admin.flag.column.value')}</Table.Th>
              <Table.Th>{t('admin.flag.column.score')}</Table.Th>
              <Table.Th>{t('admin.flag.column.sort')}</Table.Th>
              <Table.Th>{t('admin.flag.column.actions')}</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {flags.map((flag) => (
              <Table.Tr key={flag.id}>
                <Table.Td>
                  <Text size="sm" ff="monospace">{flag.flagValue}</Text>
                </Table.Td>
                <Table.Td>
                  <Badge variant="light" color="green">+{flag.score}</Badge>
                </Table.Td>
                <Table.Td>{flag.sortOrder}</Table.Td>
                <Table.Td>
                  <Group gap="xs">
                    <ActionIcon
                      variant="subtle"
                      size="sm"
                      onClick={() => openEdit(flag)}
                    >
                      <Icon path={mdiPencil} size={0.8} />
                    </ActionIcon>
                    <ActionIcon
                      variant="subtle"
                      color="red"
                      size="sm"
                      onClick={() => handleDelete(flag)}
                    >
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
            onChange={(e) => setFormValues({ ...formValues, flagValue: e.currentTarget.value })}
            required
          />
          <NumberInput
            label={t('admin.flag.column.score')}
            value={formValues.score}
            onChange={(v) => setFormValues({ ...formValues, score: Number(v) })}
            min={1}
            required
          />
          <NumberInput
            label={t('admin.flag.column.sort')}
            value={formValues.sortOrder}
            onChange={(v) => setFormValues({ ...formValues, sortOrder: Number(v) })}
          />
          <Group justify="flex-end">
            <Button variant="default" onClick={editModalHandlers.close}>
              {t('common.content.cancel')}
            </Button>
            <Button onClick={handleSave} loading={saving}>
              {t('common.content.save')}
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  )
}
