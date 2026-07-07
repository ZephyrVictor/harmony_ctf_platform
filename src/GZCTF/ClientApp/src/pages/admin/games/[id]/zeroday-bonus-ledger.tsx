import { Badge, Card, Code, Group, Pagination, SimpleGrid, Table, Text } from '@mantine/core'
import { FC, useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { showErrorMsg } from '@Utils/Shared'
import { listScoreAdjustments, ScoreAdjustmentDto } from '@Utils/AdminScoreApi'

interface Stats {
  totalScore: number
  critical: number
  high: number
  medium: number
  low: number
}

const ZeroDayBonusLedger: FC = () => {
  const { t } = useTranslation()
  const { id } = useParams()
  const gameId = Number(id)

  const [items, setItems] = useState<ScoreAdjustmentDto[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize] = useState(20)
  const [loading, setLoading] = useState(false)
  const [stats, setStats] = useState<Stats>({ totalScore: 0, critical: 0, high: 0, medium: 0, low: 0 })

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const res = await listScoreAdjustments(gameId, {
        page, pageSize, category: 'ZeroDayBonus',
      })
      setItems(res.data)
      setTotal(res.total)

      // 加载全部统计（最多 1000 条）
      const all = await listScoreAdjustments(gameId, {
        page: 1, pageSize: 1000, category: 'ZeroDayBonus',
      })
      const s: Stats = { totalScore: 0, critical: 0, high: 0, medium: 0, low: 0 }
      all.data.forEach((a) => {
        s.totalScore += a.delta
        const info = a.relatedInfo ?? ''
        if (info.includes('Critical')) s.critical++
        else if (info.includes('High')) s.high++
        else if (info.includes('Medium')) s.medium++
        else if (info.includes('Low')) s.low++
      })
      setStats(s)
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setLoading(false)
    }
  }, [gameId, page, pageSize, t])

  useEffect(() => { load() }, [load])

  const StatCard = ({ label, value, color }: { label: string; value: number; color?: string }) => (
    <Card withBorder p="sm" bg={color ? `${color}.0` : undefined}>
      <Text size="xs" c="dimmed">{label}</Text>
      <Text size="xl" fw={700} c={color}>{value}</Text>
    </Card>
  )

  return (
    <WithGameEditTab>
      <Card withBorder p="lg">
        <SimpleGrid cols={{ base: 2, md: 5 }} mb="md">
          <StatCard label={t('admin.zeroday_bonus.total_score')} value={stats.totalScore} color="green" />
          <StatCard label={t('admin.zeroday_bonus.critical')} value={stats.critical} color="red" />
          <StatCard label={t('admin.zeroday_bonus.high')} value={stats.high} color="orange" />
          <StatCard label={t('admin.zeroday_bonus.medium')} value={stats.medium} color="yellow" />
          <StatCard label={t('admin.zeroday_bonus.low')} value={stats.low} color="blue" />
        </SimpleGrid>

        <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>{t('admin.score_ledger.time')}</Table.Th>
              <Table.Th>{t('admin.score_ledger.team')}</Table.Th>
              <Table.Th>{t('admin.score_ledger.delta')}</Table.Th>
              <Table.Th>{t('admin.zeroday_bonus.cve_severity')}</Table.Th>
              <Table.Th>{t('admin.score_ledger.reason')}</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {items.length === 0 && !loading && (
              <Table.Tr>
                <Table.Td colSpan={5} align="center" style={{ opacity: 0.6 }}>
                  {t('admin.zeroday_bonus.empty')}
                </Table.Td>
              </Table.Tr>
            )}
            {items.map((a) => (
              <Table.Tr key={a.id}>
                <Table.Td>{new Date(a.createdAt).toLocaleString()}</Table.Td>
                <Table.Td><Code>#{a.teamId}</Code></Table.Td>
                <Table.Td><Text c="green" fw={700}>+{a.delta}</Text></Table.Td>
                <Table.Td>
                  {(a.relatedInfo ?? '').split(/[,;]/).map((s, i) => s.trim()).filter(Boolean).map((s, i) => (
                    <Badge key={i} size="sm" variant="light" mr={4}>{s}</Badge>
                  ))}
                </Table.Td>
                <Table.Td>{a.reason}</Table.Td>
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

export default ZeroDayBonusLedger
