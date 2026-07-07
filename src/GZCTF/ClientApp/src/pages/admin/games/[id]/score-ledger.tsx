import { Badge, Card, Code, Group, Pagination, Select, Stack, Table, Text } from '@mantine/core'
import { FC, useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { showErrorMsg } from '@Utils/Shared'
import { listScoreAdjustments, ScoreAdjustmentDto } from '@Utils/AdminScoreApi'

const ScoreLedger: FC = () => {
  const { t } = useTranslation()
  const { id } = useParams()
  const gameId = Number(id)

  const [items, setItems] = useState<ScoreAdjustmentDto[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize] = useState(20)
  const [category, setCategory] = useState<string>('')
  const [loading, setLoading] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const res = await listScoreAdjustments(gameId, {
        page, pageSize,
        category: category ? (category as 'Manual' | 'ZeroDayBonus') : undefined,
      })
      setItems(res.data)
      setTotal(res.total)
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setLoading(false)
    }
  }, [gameId, page, pageSize, category, t])

  useEffect(() => { load() }, [load])

  return (
    <WithGameEditTab>
      <Card withBorder p="lg">
        <Group mb="md">
          <Select
            label={t('admin.score_ledger.filter_category')}
            data={[
              { value: '', label: t('admin.score_ledger.all') },
              { value: 'Manual', label: t('admin.score_ledger.manual') },
              { value: 'ZeroDayBonus', label: t('admin.score_ledger.zero_day') },
            ]}
            value={category}
            onChange={(v) => { setCategory(v ?? ''); setPage(1) }}
            style={{ width: 220 }}
          />
        </Group>

        <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>{t('admin.score_ledger.time')}</Table.Th>
              <Table.Th>{t('admin.score_ledger.team')}</Table.Th>
              <Table.Th>{t('admin.score_ledger.delta')}</Table.Th>
              <Table.Th>{t('admin.score_ledger.reason')}</Table.Th>
              <Table.Th>{t('admin.score_ledger.category')}</Table.Th>
              <Table.Th>{t('admin.score_ledger.related')}</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {items.length === 0 && !loading && (
              <Table.Tr>
                <Table.Td colSpan={6} align="center" style={{ opacity: 0.6 }}>
                  {t('admin.score_ledger.empty')}
                </Table.Td>
              </Table.Tr>
            )}
            {items.map((a) => (
              <Table.Tr key={a.id}>
                <Table.Td>{new Date(a.createdAt).toLocaleString()}</Table.Td>
                <Table.Td><Code>#{a.teamId}</Code></Table.Td>
                <Table.Td>
                  <Text c={a.delta > 0 ? 'green' : 'red'} fw={700}>
                    {a.delta > 0 ? `+${a.delta}` : a.delta}
                  </Text>
                </Table.Td>
                <Table.Td>{a.reason}</Table.Td>
                <Table.Td>
                  <Badge color={a.category === 'ZeroDayBonus' ? 'grape' : 'blue'}>
                    {a.category === 'ZeroDayBonus'
                      ? t('admin.score_ledger.zero_day')
                      : t('admin.score_ledger.manual')}
                  </Badge>
                </Table.Td>
                <Table.Td>{a.relatedInfo ?? '-'}</Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>

        <Group justify="center" mt="md">
          <Pagination total={Math.ceil(total / pageSize)} value={page} onChange={setPage} />
        </Group>
      </Card>
    </WithGameEditTab>
  )
}

export default ScoreLedger
