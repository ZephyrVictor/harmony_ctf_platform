import {
  Badge,
  Button,
  Center,
  Code,
  Grid,
  Group,
  Modal,
  NumberInput,
  Pagination,
  ScrollArea,
  Select,
  Stack,
  Table,
  Text,
  Textarea,
  Title,
} from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import dayjs from 'dayjs'
import { FC, useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { getInputNumber, showErrorMsg, showSuccessMsg } from '@Utils/Shared'
import { approveTicket, getAllTickets, getSettings, rejectTicket, TicketDto } from '@Utils/ExtApi'

const TicketStatusOptions = [
  { value: '', label: 'All' },
  { value: 'Pending', label: 'Pending' },
  { value: 'Approved', label: 'Approved' },
  { value: 'Rejected', label: 'Rejected' },
]

const AdminTickets: FC = () => {
  const { t } = useTranslation()
  const { id } = useParams()
  const gameId = Number(id)
  const [tickets, setTickets] = useState<TicketDto[]>([])
  const [loading, setLoading] = useState(true)
  const [statusFilter, setStatusFilter] = useState<string>('')
  const [page, setPage] = useState(1)
  const perPage = 20

  const [detailModal, detailModalHandlers] = useDisclosure(false)
  const [selectedTicket, setSelectedTicket] = useState<TicketDto | null>(null)
  const [refundScore, setRefundScore] = useState(0)
  const [bonusScore, setBonusScore] = useState(0)
  const [comment, setComment] = useState('')
  const [saving, setSaving] = useState(false)

  const load = useCallback(async () => {
    try {
      const data = await getAllTickets(statusFilter || undefined, 200, 0, gameId)
      setTickets(data)
    } catch (err: any) {
      showErrorMsg(err, t)
    } finally {
      setLoading(false)
    }
  }, [gameId, statusFilter, t])

  useEffect(() => {
    load()
  }, [load])

  const openDetail = async (ticket: TicketDto) => {
    setSelectedTicket(ticket)
    setComment('')
    detailModalHandlers.open()

    try {
      const settings = await getSettings()
      const enableAutoRefund = settings['Ticket.EnableAutoRefund'] === 'true'
      if (enableAutoRefund) {
        const ratio = Number(settings['Ticket.DefaultRefundRatio']) || 1.0
        setRefundScore(Math.round(ticket.penaltyScore * ratio))
      } else {
        setRefundScore(0)
      }
    } catch {
      setRefundScore(0)
    }
    setBonusScore(0)
  }

  const handleApprove = async () => {
    if (!selectedTicket) return
    setSaving(true)
    try {
      await approveTicket(selectedTicket.id, {
        refundScore,
        bonusScore,
        comment: comment || undefined,
      })
      showSuccessMsg(t('common.content.save_success'), t)
      detailModalHandlers.close()
      await load()
    } catch (err: any) {
      showErrorMsg(err, t)
    } finally {
      setSaving(false)
    }
  }

  const handleReject = async () => {
    if (!selectedTicket) return
    if (!comment.trim()) {
      showErrorMsg(new Error(t('admin.ticket.comment_required')), t)
      return
    }
    setSaving(true)
    try {
      await rejectTicket(selectedTicket.id, { comment: comment.trim() })
      showSuccessMsg(t('common.content.save_success'), t)
      detailModalHandlers.close()
      await load()
    } catch (err: any) {
      showErrorMsg(err, t)
    } finally {
      setSaving(false)
    }
  }

  const pagedTickets = tickets.slice((page - 1) * perPage, page * perPage)

  const statusColor = (status: string) =>
    status === 'Approved' ? 'green' : status === 'Rejected' ? 'red' : 'yellow'

  return (
    <WithGameEditTab
      isLoading={loading}
      head={
        <Group justify="space-between" w="100%">
          <Title order={3}>{t('admin.ticket.title')}</Title>
          <Select
            w="10rem"
            data={TicketStatusOptions.map((o) => ({ value: o.value, label: o.label }))}
            value={statusFilter}
            onChange={(v) => { setStatusFilter(v ?? ''); setPage(1) }}
          />
        </Group>
      }
    >
      {tickets.length === 0 && !loading ? (
        <Center h="10rem">
          <Text c="dimmed">{t('admin.ticket.empty')}</Text>
        </Center>
      ) : (
        <>
          <ScrollArea type="never" h="calc(100vh - 16rem)">
            <Table highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>{t('admin.ticket.col.user')}</Table.Th>
                  <Table.Th>{t('admin.ticket.col.challenge')}</Table.Th>
                  <Table.Th>{t('admin.ticket.col.answer')}</Table.Th>
                  <Table.Th>{t('admin.ticket.col.description')}</Table.Th>
                  <Table.Th>{t('admin.ticket.col.status')}</Table.Th>
                  <Table.Th>{t('admin.ticket.col.time')}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {pagedTickets.map((ticket) => (
                  <Table.Tr
                    key={ticket.id}
                    style={{ cursor: 'pointer' }}
                    onClick={() => openDetail(ticket)}
                  >
                    <Table.Td>
                      <Text size="sm">{ticket.userName ?? '—'}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{ticket.challengeName ?? '—'}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Code>{ticket.answer ?? '—'}</Code>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" lineClamp={2}>{ticket.description}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Badge color={statusColor(ticket.status)} variant="light">
                        {ticket.status}
                      </Badge>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{dayjs(ticket.createdAt).format('L LT')}</Text>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </ScrollArea>
          <Pagination
            value={page}
            onChange={setPage}
            total={Math.ceil(tickets.length / perPage)}
          />
        </>
      )}

      <Modal
        opened={detailModal}
        onClose={detailModalHandlers.close}
        title={t('admin.ticket.detail_title')}
        size="lg"
      >
        {selectedTicket && (
          <Stack gap="md">
            <Grid>
              <Grid.Col span={6}>
                <Text size="sm" c="dimmed">{t('admin.ticket.col.user')}</Text>
                <Text>{selectedTicket.userName ?? '—'}</Text>
              </Grid.Col>
              <Grid.Col span={6}>
                <Text size="sm" c="dimmed">{t('admin.ticket.col.challenge')}</Text>
                <Text>{selectedTicket.challengeName ?? '—'}</Text>
              </Grid.Col>
              <Grid.Col span={6}>
                <Text size="sm" c="dimmed">{t('admin.ticket.col.answer')}</Text>
                <Code block>{selectedTicket.answer ?? '—'}</Code>
              </Grid.Col>
              <Grid.Col span={6}>
                <Text size="sm" c="dimmed">{t('admin.ticket.col.penalty_score')}</Text>
                <Text fw="bold" c="red">-{selectedTicket.penaltyScore}</Text>
              </Grid.Col>
              <Grid.Col span={6}>
                <Text size="sm" c="dimmed">{t('admin.ticket.col.status')}</Text>
                <Badge color={statusColor(selectedTicket.status)} variant="light" mt={4}>
                  {selectedTicket.status}
                </Badge>
              </Grid.Col>
              <Grid.Col span={6}>
                <Text size="sm" c="dimmed">{t('admin.ticket.col.time')}</Text>
                <Text>{dayjs(selectedTicket.createdAt).format('L LTS')}</Text>
              </Grid.Col>
              <Grid.Col span={12}>
                <Text size="sm" c="dimmed">{t('admin.ticket.col.description')}</Text>
                <Text mt={4} style={{ whiteSpace: 'pre-wrap' }}>{selectedTicket.description}</Text>
              </Grid.Col>
            </Grid>

            {selectedTicket.status === 'Approved' || selectedTicket.status === 'Rejected' ? (
              <Stack gap="xs">
                <Text size="sm" fw="bold">{t('admin.ticket.review_result')}</Text>
                <Grid>
                  <Grid.Col span={6}>
                    <Text size="sm" c="dimmed">{t('admin.ticket.reviewer')}</Text>
                    <Text>{selectedTicket.reviewerName ?? '—'}</Text>
                  </Grid.Col>
                  <Grid.Col span={6}>
                    <Text size="sm" c="dimmed">{t('admin.ticket.review_time')}</Text>
                    <Text>{selectedTicket.reviewedAt ? dayjs(selectedTicket.reviewedAt).format('L LTS') : '—'}</Text>
                  </Grid.Col>
                  {selectedTicket.status === 'Approved' && (
                    <>
                      <Grid.Col span={6}>
                        <Text size="sm" c="dimmed">{t('admin.ticket.refund_score')}</Text>
                        <Text c="green">+{selectedTicket.refundScore}</Text>
                      </Grid.Col>
                      <Grid.Col span={6}>
                        <Text size="sm" c="dimmed">{t('admin.ticket.bonus_score')}</Text>
                        <Text c="green">+{selectedTicket.bonusScore}</Text>
                      </Grid.Col>
                    </>
                  )}
                  <Grid.Col span={12}>
                    <Text size="sm" c="dimmed">{t('admin.ticket.comment')}</Text>
                    <Text mt={4}>{selectedTicket.reviewComment || '—'}</Text>
                  </Grid.Col>
                </Grid>
              </Stack>
            ) : (
              <Stack gap="md">
                <Textarea
                  label={t('admin.ticket.comment')}
                  description={t('admin.ticket.comment_desc')}
                  value={comment}
                  onChange={(e) => setComment(e.currentTarget.value)}
                  minRows={3}
                  required
                  placeholder={t('admin.ticket.comment_placeholder')}
                />
                <Grid>
                  <Grid.Col span={6}>
                    <NumberInput
                      label={t('admin.ticket.refund_score')}
                      value={refundScore}
                      onChange={(v) => {
                        const num = getInputNumber(v)
                        if (isNaN(num)) return
                        setRefundScore(num)
                      }}
                      min={0}
                    />
                  </Grid.Col>
                  <Grid.Col span={6}>
                    <NumberInput
                      label={t('admin.ticket.bonus_score')}
                      value={bonusScore}
                      onChange={(v) => {
                        const num = getInputNumber(v)
                        if (isNaN(num)) return
                        setBonusScore(num)
                      }}
                      min={0}
                    />
                  </Grid.Col>
                </Grid>
                <Group justify="flex-end">
                  <Button variant="default" onClick={detailModalHandlers.close}>
                    {t('common.content.cancel')}
                  </Button>
                  <Button color="red" variant="outline" onClick={handleReject} loading={saving}>
                    {t('admin.ticket.reject')}
                  </Button>
                  <Button color="green" onClick={handleApprove} loading={saving}>
                    {t('admin.ticket.approve')}
                  </Button>
                </Group>
              </Stack>
            )}
          </Stack>
        )}
      </Modal>
    </WithGameEditTab>
  )
}

export default AdminTickets
